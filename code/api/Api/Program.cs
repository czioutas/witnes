using Api.Application.Services;
using Api.Data;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        IHost host = CreateHostBuilder(args).Build();

        IHostEnvironment? env = host.Services.GetService<IHostEnvironment>();

        if (env is null)
        {
            return;
        }

        var configuration = new ConfigurationBuilder()
        .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();

        // read config and adjust the logger
        var logtailSecret = configuration.GetValue<string>("LogtailSecret");
        var logtailHost = configuration.GetValue<string>("LogtailHost");

        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty("Environment", env.EnvironmentName)
            .Enrich.WithProperty("AppName", env.ApplicationName)
            .Enrich.FromLogContext()
            .WriteTo.BetterStack(
                sourceToken: logtailSecret ?? "",
                betterStackEndpoint: logtailHost ?? "");

        Log.Logger = loggerConfig.CreateLogger();

        Log.Information("ENV: " + env.EnvironmentName);

        // startup
        try
        {
            Log.Information("Application Starting up");

            using (var scope = host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

                // Drop database if running in Integration environment and AlwaysDropDb is enabled
                if (env.EnvironmentName == "Integration" &&
                    configuration.GetValue<bool>("IntegrationTestSettings:AlwaysDropDb", false))
                {
                    Log.Information("Dropping database for integration testing...");
                    dbContext?.Database.EnsureDeleted();
                    dbContext?.Database.EnsureCreated();
                    Log.Information("Database dropped.");
                }
                else
                {
                    Log.Information("Applying migration to application database.");
                    dbContext?.Database.Migrate();
                    Log.Information("Done...");
                }

                Log.Information("Seeding Default Data...");
                await Seed.BaseSeedAsync(scope.ServiceProvider);

                if (env.IsDevelopment() || env.EnvironmentName == "Local")
                {
                    Log.Information("Seeding Development Data...");
                    await Seed.DevelopmentSeedAsync(scope.ServiceProvider);
                }
                else
                {
                    await Seed.ProductionSeedAsync(scope.ServiceProvider);
                }

                Log.Information("Done...");
            }

            host.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Log.Fatal(ex, "The application failed to start correctly. " + ex.Message);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration))
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
