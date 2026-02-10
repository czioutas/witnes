using System.IO.Compression;
using Api.Application;
using Api.Application.Account.Services;
using Api.Application.Account.Services.Interfaces;
using Api.Application.Communication.Services;
using Api.Application.Converters;
using Api.Application.Extensions;
using Api.Application.Features.Services;
using Api.Application.Filters;
using Api.Application.HealthChecks;
using Api.Application.Limits.Services;
using Api.Application.Metrics;
using Api.Application.Middleware;
using Api.Application.Pricing.Services;
using Api.Application.ProjectKeys;
using Api.Application.ProjectKeys.Services;
using Api.Application.Settings;
using Api.Application.Tenancy;
using Api.Application.Tenancy.Services;
using Api.Application.Transformers;
using Api.Settings.Settings;
using Coravel;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using StackExchange.Redis;
using Api.Product.DailySalt;
using Api.Product.Visitors;
using Api.Product.PageLoads;
using Usersr.API.Users.Services;
using Api.Product.MetricsProcessing.Gold;
using Api.Product.MetricsProcessing.Silver;
using Api.Product.MetricsProcessing.Bronze;
using Api.Product.Metrics;

namespace Api;

public class Startup
{
    public IConfiguration Configuration { get; }

    public static readonly ILoggerFactory ConsoleLoggerFactory =
        LoggerFactory.Create(builder => builder.AddConsole());

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.SetupDatabase(Configuration);
        services.SetupCache(Configuration);
        services.SetupFileStorage(Configuration);
        services.SetupApiUsageTracking();
        services.SetupIdentity();

        services.AddAutoMapper(typeof(Startup));
        AutoDiscover(services, Configuration);

        // Feature Management
        services.AddSingleton<IFeatureDefinitionProvider, DatabaseFeatureDefinitionProvider>();
        services.AddFeatureManagement();

        services.AddScheduler();

        IConfigurationSection corsSettingsSection = Configuration.GetSection(nameof(CorsSettings));
        services.Configure<CorsSettings>(corsSettingsSection);

        CorsSettings? corsSettings = corsSettingsSection.Get<CorsSettings>();

        var origins = corsSettings?.Origins?.Split(",") ?? [];
        Console.WriteLine($"[CORS] Configured origins ({origins.Length}): [{string.Join("] [", origins)}]");

        services.AddCors(options =>
            {
                options.AddPolicy("DefaultPolicy",
                    builder =>
                    {
                        builder
                            .SetIsOriginAllowedToAllowWildcardSubdomains()
                            .WithOrigins(origins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials()
                            .WithExposedHeaders("Content-Disposition");
                    });
            });

        services.AddRouting(options => options.LowercaseUrls = true);

        services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.AddControllers(options =>
        {
            options.Filters.Add<CustomExceptionFilter>();
            options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));

        }).AddJsonOptions(opt =>
        {
            // Add custom enum converter factory that respects [EnumMember] attributes for all enums
            opt.JsonSerializerOptions.Converters.Add(new EnumMemberJsonConverterFactory());
            opt.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
        });

        services.AddJwtAuthentication(Configuration);

        SetupHealthChecks(services);

        services.SetupSwagger();

        services.SetupMassTransit(Configuration);

        // Configure OpenTelemetry metrics and tracing
        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();

                // Add custom application metrics
                metrics.AddMeter("Witnes.Api");

                // Configure Prometheus exporter with custom histogram buckets
                // Buckets: 0, 100ms, 500ms, 1s, 3s, 5s, 30s, inf
                metrics.AddPrometheusExporter(options =>
                {
                    options.ScrapeResponseCacheDurationMilliseconds = 0;
                });

                // Add custom histogram buckets view for HTTP requests
                metrics.AddView("http.server.request.duration", new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = [0, 0.1, 0.5, 1, 3, 5, 30]
                });

                // Add custom histogram buckets view for activity emissions calculations
                // Buckets: 0, 10ms, 50ms, 100ms, 250ms, 500ms, 1000ms, 5000ms, 10000ms, inf
                metrics.AddView(ApplicationMetrics.ActivityEmissionsCalculationDurationMetricName, new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = [0, 10, 50, 100, 250, 500, 1000, 5000, 10000]
                });
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddEntityFrameworkCoreInstrumentation();
            });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
    {
        serviceProvider.ScheduleJobs();

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseSerilogRequestLogging();

        app.UseRouting();

        app.UseCors("DefaultPolicy");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseSwaggerUI(x => { x.SwaggerEndpoint("/swagger/v1/swagger.yaml", "Witnes API"); });

        app.UseResponseCompression();

        app.UseMiddleware<TraceMiddleware>();

        // Use ProjectKeyMiddleware ONLY for events routes
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments("/api/v1/events"),
            appBuilder =>
            {
                appBuilder.UseMiddleware<ProjectKeyMiddleware>();
            });

        // Use MultiTenantServiceMiddleware for all routes EXCEPT events
        app.UseWhen(
            context => !context.Request.Path.StartsWithSegments("/api/v1/events"),
            appBuilder =>
            {
                appBuilder.UseMiddleware<MultiTenantServiceMiddleware>();
            });

        app.UseOutputCache();

        app.SetupHealthCheckEndpoints();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // OpenTelemetry Prometheus metrics endpoint
            endpoints.MapPrometheusScrapingEndpoint();
        });
    }

    public virtual void SetupHealthChecks(IServiceCollection services)
    {
        // Registers required services for health checks
        // Redis uses the IConnectionMultiplexer singleton registered in SetupCache
        services.AddHealthChecks()
            .AddCheck<DbContextHealthCheck>(nameof(DbContextHealthCheck))
            .AddRedis(sp => sp.GetRequiredService<IConnectionMultiplexer>(), name: "Redis");
    }

    public virtual void AutoDiscover(IServiceCollection services, IConfiguration configuration)
    {
        SetupSettingsModels(services, configuration);

        services.AddSingleton<ILinkResolver, LinkResolver>();
        services.AddSingleton<ICommunicationService, CommunicationService>();
        services.AddTransient<ITokenService, TokenService>();
        services.AddTransient<IRoleService, RoleService>();
        services.AddTransient<ITenantService, TenantService>();
        services.AddTransient<IAccountService, AccountService>();
        services.AddTransient<IUsersService, UsersService>();
        services.AddTransient<IVisitorsService, VisitorsService>();
        services.AddTransient<IPageLoadsService, PageLoadsService>();
        services.AddTransient<ITenantPricingService, TenantPricingService>();
        services.AddScoped<ILimitsService, LimitsService>();
        services.AddScoped<IRequestTenant, RequestTenant>();
        services.AddScoped<MultiTenantServiceMiddleware>();

        // Project Keys
        services.AddScoped<ProjectKeyMiddleware>();
        services.AddTransient<IProjectKeyService, ProjectKeyService>();

        // Daily salt & visitor hashing
        services.AddSingleton<IDailySaltService, DailySaltService>();
        services.AddTransient<IVisitorHashService, VisitorHashService>();

        // Witnes services
        services.AddTransient<IBronzeService, BronzeService>();
        services.AddTransient<ISilverService, SilverService>();
        services.AddTransient<IGoldService, GoldService>();
        services.AddTransient<IMetricsService, MetricsService>();
    }

    protected virtual void SetupSettingsModels(IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection tokenProviderSettingsSection = Configuration.GetSection(nameof(TokenProviderSettings));
        services.Configure<TokenProviderSettings>(tokenProviderSettingsSection);

        IConfigurationSection smtpSettingsSection = Configuration.GetSection(nameof(SmtpSettings));
        services.Configure<SmtpSettings>(smtpSettingsSection);

        IConfigurationSection applicationSettings = Configuration.GetSection(nameof(ApplicationSettings));
        services.Configure<ApplicationSettings>(applicationSettings);

        IConfigurationSection rabbitMQSettingsSection = Configuration.GetSection(nameof(RabbitMQSettings));
        services.Configure<RabbitMQSettings>(rabbitMQSettingsSection);
    }
}
