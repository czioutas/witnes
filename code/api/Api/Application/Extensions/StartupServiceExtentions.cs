using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Api.Application.Account.Entities;
using Api.Application.Filters;
using Api.Application.Services;
using Api.Application.Settings;
using Api.Data;
using Libs.Domain;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Minio;
using RabbitMQ.Client;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZiggyCreatures.Caching.Fusion;

namespace Api.Application.Extensions;

public static class StartupServiceExtensions
{
    public static void SetupCache(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection redisSettingsSection = configuration.GetSection(nameof(RedisSettings));
        RedisSettings? redisSettings = redisSettingsSection.Get<RedisSettings>();

        var connectionString = $"{redisSettings?.IpAddress}:{redisSettings?.Port}";

        // Add SSL for production cloud environments (ElastiCache requires it)
        // Skip SSL for local docker containers
        // var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        // var useSsl = redisSettings?.UseSsl ?? false;
        // if (environment == "Production" && useSsl)
        // {
        //     connectionString += ",ssl=true";
        // }

        connectionString += ",abortConnect=false";

        if (!string.IsNullOrEmpty(redisSettings?.Password))
        {
            connectionString += $",password={redisSettings.Password}";
        }

        Console.WriteLine($"Redis Connection: {connectionString}");

        // Register ConnectionMultiplexer as singleton
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            return ConnectionMultiplexer.Connect(connectionString);
        });

        // Also use the same configuration for the distributed cache
        services.AddStackExchangeRedisCache(action =>
        {
            action.Configuration = connectionString;
        });

        services.AddFusionCache()
            .WithDistributedCache(provider =>
                provider.GetRequiredService<IDistributedCache>()
            ).WithOptions(options =>
            {
                options.DefaultEntryOptions.IsFailSafeEnabled = false;
                options.DefaultEntryOptions.AllowBackgroundBackplaneOperations = true;
            })
            .WithNewtonsoftJsonSerializer();

        services.Configure<FusionCacheEntryOptions>(options =>
        {
            options.Duration = TimeSpan.FromHours(1);
            options.IsFailSafeEnabled = true;
            options.FailSafeMaxDuration = TimeSpan.FromHours(2);
            options.Priority = Microsoft.Extensions.Caching.Memory.CacheItemPriority.High;
        });

        services.AddStackExchangeRedisOutputCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = "GreenactionsOutputCache";
        });

        services.AddOutputCache(options =>
        {
            options.AddPolicy(Constants.OutputCachePolicyFiveMinutes, builder =>
                builder.Expire(TimeSpan.FromMinutes(5)));
            options.AddPolicy(Constants.OutputCachePolicyOneHour, builder =>
                builder.Expire(TimeSpan.FromHours(1)));
            options.AddPolicy(Constants.OutputCachePolicyTwoDays, builder =>
                builder.Expire(TimeSpan.FromDays(2)));
            options.AddPolicy(Constants.OutputCachePolicyTenDaysByKey, builder =>
                builder.Expire(TimeSpan.FromDays(10)).SetVaryByRouteValue("key"));
            options.AddPolicy(Constants.OutputCachePolicyTenDays, builder =>
                builder.Expire(TimeSpan.FromDays(10)));
        });
    }

    public static void SetupApiUsageTracking(this IServiceCollection services)
    {
    }

    public static void SetupDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        // NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

        var primaryConnectionString = configuration.GetConnectionString("Primary");
        var replicaConnectionString = configuration.GetConnectionString("Replica");

        Console.WriteLine($"PRIMARY DB: {primaryConnectionString}");
        Console.WriteLine($"REPLICA DB: {replicaConnectionString}");

        // Configure connection pooling for primary (write) database
        var primaryConnectionBuilder = new Npgsql.NpgsqlConnectionStringBuilder(primaryConnectionString)
        {
            MaxPoolSize = 20,           // Maximum connections in the pool
            MinPoolSize = 5,            // Minimum connections kept open
            ConnectionIdleLifetime = 60, // Close idle connections after 60 seconds
            ConnectionPruningInterval = 10, // Check for idle connections every 10 seconds
            Timeout = 30,                // Connection timeout in seconds
            CommandTimeout = 30          // Command timeout in seconds
        };

        // Configure connection pooling for replica (read) database
        var replicaConnectionBuilder = new Npgsql.NpgsqlConnectionStringBuilder(replicaConnectionString)
        {
            MaxPoolSize = 20,           // Maximum connections in the pool
            MinPoolSize = 5,            // Minimum connections kept open
            ConnectionIdleLifetime = 60, // Close idle connections after 60 seconds
            ConnectionPruningInterval = 10, // Check for idle connections every 10 seconds
            Timeout = 30,                // Connection timeout in seconds
            CommandTimeout = 30          // Command timeout in seconds
        };

        var optimizedPrimaryConnectionString = primaryConnectionBuilder.ToString();
        var optimizedReplicaConnectionString = replicaConnectionBuilder.ToString();

        // Register WRITE (primary) context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(optimizedPrimaryConnectionString, o =>
            {
                o.MapEnum<FeatureKey>("feature_key");
                o.MapEnum<LimitKey>("limit_key");
                o.MapEnum<LimitPeriod>("limit_period");
            })
        );

        // Register READ (replica) context
        services.AddDbContext<ApplicationDbContextRead>(options =>
            options.UseNpgsql(optimizedReplicaConnectionString, o =>
            {
                o.MapEnum<FeatureKey>("feature_key");
                o.MapEnum<LimitKey>("limit_key");
                o.MapEnum<LimitPeriod>("limit_period");
            })
        );

        // services.AddLogging(loggingBuilder =>
        // {
        // loggingBuilder.AddConsole()
        //     .AddFilter(DbLoggerCategory.Database.Command.Name, LogLevel.Information);
        // loggingBuilder.AddDebug();
        // });
    }

    public static void SetupSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SchemaFilter<SnakeCaseSchemaFilter>();

            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Witnes Server API",
                Version = "v1",
                Description = "Witnes Server API for managing energy data and services.",
                TermsOfService = new Uri("https://witnes.io/terms"),
                Contact = new OpenApiContact
                {
                    Name = "Witnes",
                    Email = "hello@witnes.io",
                    Url = new Uri("https://witnes.io/#contact"),
                },
            });

            c.SupportNonNullableReferenceTypes();

            c.UseAllOfToExtendReferenceSchemas();

            // Set the comments path for the Swagger JSON and UI.
            var xfile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xpath = Path.Combine(AppContext.BaseDirectory, xfile);
            if (File.Exists(xpath))
            {
                c.IncludeXmlComments(xpath);
            }
            else
            {
                Console.WriteLine($"⚠️ XML comments file not found at {xpath}");
            }
        });
    }

    public static void SetupHealthCheckEndpoints(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health", new HealthCheckOptions()
        {
            // WriteResponse is a delegate used to write the response.
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status500InternalServerError,
                [HealthStatus.Unhealthy] = StatusCodes.Status500InternalServerError
            },
            ResponseWriter = HttpContextExtensions.WriteHealthReportResponse
        });
        app.UseHealthChecks("/hcpro",
            new HealthCheckOptions()
            {
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status500InternalServerError,
                    [HealthStatus.Unhealthy] = StatusCodes.Status500InternalServerError
                },
                ResponseWriter = HttpContextExtensions.WritePrometheusHealthReport
            });
    }

    public static void SetupFileStorage(this IServiceCollection services, IConfiguration configuration)
    {

    }

    public static void SetupIdentity(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUserEntity, ApplicationRoleEntity>(options =>
        {
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
            options.Password.RequireUppercase = true;
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
    }

    public static void AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        TokenProviderSettings _tps = new TokenProviderSettings();
        var a = configuration.GetSection(nameof(TokenProviderSettings));
        configuration.GetSection(nameof(TokenProviderSettings)).Bind(_tps);

        SymmetricSecurityKey _signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_tps.SecretKey));
        SigningCredentials _sc = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        _tps.SigningCredentials = _sc;

        services.AddSingleton<TokenProviderSettings>(_tps);

        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddScheme<JwtBearerOptions, CustomJwtBearerHandler>(JwtBearerDefaults.AuthenticationScheme, options => { });
    }


    // public static IServiceCollection AddMailerSendServices(
    //         this IServiceCollection services,
    //         IConfiguration configuration)
    // {
    //     services.Configure<SmtpSettings>(
    //         configuration.GetSection("SmtpSettings"));

    //     services.AddHttpClient(CommunicationService.HttpClientName, client =>
    //     {
    //         client.BaseAddress = new Uri("https://api.mailersend.com/v1/");
    //         client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    //         client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

    //         var settings = configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
    //         if (settings?.ApiKey != null)
    //         {
    //             client.DefaultRequestHeaders.Authorization =
    //                 new AuthenticationHeaderValue("Bearer", settings.ApiKey);
    //         }
    //     })
    //     .SetHandlerLifetime(TimeSpan.FromMinutes(5))  // Default lifetime is 2 minutes
    //     .AddPolicyHandler(GetRetryPolicy())
    //     .AddPolicyHandler(GetCircuitBreakerPolicy());

    //     services.AddScoped<ICommunicationService, CommunicationService>();

    //     return services;
    // }

    // private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    // {
    //     return HttpPolicyExtensions
    //         .HandleTransientHttpError()
    //         .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    //         .WaitAndRetryAsync(3, retryAttempt =>
    //             TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    // }

    // private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    // {
    //     return HttpPolicyExtensions
    //         .HandleTransientHttpError()
    //         .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    // }

    /// <summary>
    /// Creates a RabbitMQ ConnectionFactory from configuration settings
    /// </summary>
    public static ConnectionFactory CreateRabbitMQConnectionFactory(IConfiguration configuration)
    {
        var rabbitMQSettings = configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();
        var factory = new ConnectionFactory
        {
            HostName = rabbitMQSettings?.Host ?? "localhost",
            Port = rabbitMQSettings?.Port ?? 5672,
            UserName = rabbitMQSettings?.Username ?? "guest",
            Password = rabbitMQSettings?.Password ?? "guest",
            VirtualHost = rabbitMQSettings?.VirtualHost ?? "/"
        };

        if (rabbitMQSettings?.Port == 5671)
        {
            factory.Ssl = new SslOption
            {
                Enabled = true,
                ServerName = rabbitMQSettings.Host
            };
        }

        return factory;
    }

    /// <summary>
    /// Configures MassTransit for distributed messaging with RabbitMQ
    /// </summary>
    public static void SetupMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMQSettings = configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();

        services.AddMassTransit(x =>
        {
            // Auto-register all consumers from assembly
            x.AddConsumers(typeof(Startup).Assembly);

            x.UsingRabbitMq((context, cfg) =>
            {
                var protocol = rabbitMQSettings?.Port == 5671 ? "amqps" : "amqp";
                var host = rabbitMQSettings?.Host ?? "localhost";
                var port = rabbitMQSettings?.Port ?? 5672;
                var vhost = rabbitMQSettings?.VirtualHost ?? "/";
                var username = rabbitMQSettings?.Username ?? "guest";
                var password = rabbitMQSettings?.Password ?? "guest";

                var connectionString = $"{protocol}://{username}:{password}@{host}:{port}{vhost}";

                cfg.Host(new Uri(connectionString));

                // Global concurrency settings
                cfg.PrefetchCount = 16; // Max 16 messages prefetched per queue
                cfg.ConcurrentMessageLimit = 50; // Max 50 concurrent messages across ALL queues

                // Auto-create queues for each consumer
                cfg.ConfigureEndpoints(context);
            });
        });
    }
}