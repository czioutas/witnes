#pragma warning disable

using Api.Application.Account.Entities;
using Api.Application.Account.Models;
using Api.Application.Account.Services.Interfaces;
using Api.Application.Authentication;
using Api.Application.Limits.Entities;
using Api.Application.Tenancy.Services;
using Api.Data;
using Api.Data.Seeders;
using Libs.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class Seed
{
    private static Random random = new Random();
    private static readonly string password = "aA!1aA!1aA!1";
    /// <summary>
    /// Seed used for any environment
    /// </summary>
    /// <param name="services">The ServiceProvider</param>
    /// <returns></returns>
    public static async Task BaseSeedAsync(IServiceProvider services)
    {
        await InsertDefaultRolesAsync(services);
        await InsertDemoUserAsync(services);
        await InsertDemoNonAdminUserAsync(services);
        return;
    }

    /// <summary>
    /// Seed used for the production environment on top of Base
    /// </summary>
    /// <param name="services">The ServiceProvider</param>
    /// <returns></returns>
    public static async Task ProductionSeedAsync(IServiceProvider services)
    {
    }

    public static async Task TestSeedAsync(IServiceProvider services)
    {
    }

    /// <summary>
    /// Seed used for the development environment on top of Base
    /// </summary>
    /// <param name="services">The ServiceProvider</param>
    /// <returns></returns>
    public static async Task DevelopmentSeedAsync(IServiceProvider services)
    {
    }

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static async Task InsertDemoUserAsync(IServiceProvider services)
    {
        var accountService = services.GetService<IAccountService>();
        var userManager = services.GetService<UserManager<ApplicationUserEntity>>();

        if (accountService == null || userManager == null)
        {
            Console.WriteLine("Account service or UserManager not found. Skipping demo user seeding.");
            return;
        }

        var demoUserEmail = "mamaslittlebakery@witnes.io";
        var existingUser = await accountService.FindByEmailAsync(demoUserEmail);
        if (existingUser != null)
        {
            Console.WriteLine("Demo user already exists. Skipping seeding.");
            return;
        }

        var registerModel = new RegisterModel
        {
            Email = demoUserEmail,
            Password = "witnesDemoAa1!",
            FirstName = "Bob",
            LastName = "Ross",
            NormalizedTenantIdentifier = "Mama's Little Bakery",
        };

        var result = await accountService.RegisterAsync(registerModel, new[] { AccountRoles.AdminUserRole }, false);

        if (result.IsFailure)
        {
            Console.WriteLine($"Error seeding demo user: {result.ErrorModel.Message}");
        }
        else
        {
            // Get the user entity to generate confirmation token
            var userEntity = await userManager.FindByIdAsync(result.GetValue.Id.ToString());
            if (userEntity != null)
            {
                // Generate email confirmation token
                var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(userEntity);

                // Confirm email for demo user
                var confirmResult = await accountService.ConfirmEmailAsync(result.GetValue.Id, confirmationToken);
                if (!confirmResult.Succeeded)
                {
                    Console.WriteLine($"Error confirming demo user email: {string.Join(", ", confirmResult.Errors.Select(e => e.Description))}");
                }
                else
                {
                    Console.WriteLine("Demo user seeded and email confirmed successfully.");

                    // Seed default limits for the demo tenant
                    await SeedTenantLimitsAsync(services, result.GetValue.TenantId);
                }
            }
            else
            {
                Console.WriteLine("Demo user created but could not be found for email confirmation.");
            }
        }
    }

    private static async Task InsertDemoNonAdminUserAsync(IServiceProvider services)
    {
        var accountService = services.GetService<IAccountService>();
        var userManager = services.GetService<UserManager<ApplicationUserEntity>>();

        if (accountService == null || userManager == null)
        {
            Console.WriteLine("Account service or UserManager not found. Skipping demo non-admin user seeding.");
            return;
        }

        var demoUserEmail = "employee@witnes.io";
        var existingUser = await accountService.FindByEmailAsync(demoUserEmail);
        if (existingUser != null)
        {
            Console.WriteLine("Demo non-admin user already exists. Skipping seeding.");
            return;
        }

        // Get the demo admin user to use the same tenant
        var adminUser = await accountService.FindByEmailAsync("mamaslittlebakery@witnes.io");
        if (adminUser == null)
        {
            Console.WriteLine("Demo admin user not found. Cannot create non-admin user. Skipping seeding.");
            return;
        }

        var registerModel = new RegisterModel
        {
            Email = demoUserEmail,
            Password = "greenactionsDemoAa1!",
            FirstName = "Jane",
            LastName = "Smith",
            NormalizedTenantIdentifier = "Mama's Little Bakery",
        };

        var result = await accountService.RegisterAsync(registerModel, new[] { AccountRoles.DefaultUserRole }, bypassTenantCheck: true);

        if (result.IsFailure)
        {
            Console.WriteLine($"Error seeding demo non-admin user: {result.ErrorModel.Message}");
        }
        else
        {
            // Get the user entity to generate confirmation token
            var userEntity = await userManager.FindByIdAsync(result.GetValue.Id.ToString());
            if (userEntity != null)
            {
                // Generate email confirmation token
                var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(userEntity);

                // Confirm email for demo user
                var confirmResult = await accountService.ConfirmEmailAsync(result.GetValue.Id, confirmationToken);
                if (!confirmResult.Succeeded)
                {
                    Console.WriteLine($"Error confirming demo non-admin user email: {string.Join(", ", confirmResult.Errors.Select(e => e.Description))}");
                }
                else
                {
                    Console.WriteLine("Demo non-admin user seeded and email confirmed successfully.");
                }
            }
            else
            {
                Console.WriteLine("Demo non-admin user created but could not be found for email confirmation.");
            }
        }
    }


    private static async Task InsertDefaultRolesAsync(IServiceProvider services)
    {
        var roleService = services.GetService<IRoleService>();
        if (roleService == null)
        {
            Console.WriteLine("Role service not found. Skipping role seeding.");
            return;
        }

        try
        {
            await roleService.AddDefaultSystemRolesAsync();
            Console.WriteLine("Default roles seeded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding default roles: {ex.Message}");
        }
    }

    private static async Task SeedTenantLimitsAsync(IServiceProvider services, Guid tenantId)
    {
        var dbContext = services.GetService<ApplicationDbContext>();
        if (dbContext == null)
        {
            Console.WriteLine("Database context not found. Skipping tenant limits seeding.");
            return;
        }

        try
        {
            // Check if limits already exist for this tenant
            var existingLimits = await dbContext.TenantLimits
                .IgnoreQueryFilters()
                .Where(l => l.TenantId == tenantId)
                .AnyAsync();

            if (existingLimits)
            {
                Console.WriteLine($"Tenant limits already exist for tenant {tenantId}. Skipping seeding.");
                return;
            }

            // Seed default limits
            var defaultLimits = new[]
            {
                new TenantLimitEntity
                {
                    TenantId = tenantId,
                    LimitKey = LimitKey.Locations,
                    Period = LimitPeriod.Monthly,
                    MaxAmount = 3,
                    CurrentAmount = 0
                }
            };

            await dbContext.TenantLimits.AddRangeAsync(defaultLimits);
            await dbContext.SaveChangesAsync();

            Console.WriteLine($"Tenant limits seeded successfully for tenant {tenantId}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding tenant limits: {ex.Message}");
        }
    }


    // private static async Task InsertTsosFromCSVAsync(IServiceProvider services)
    // {
    //     _dbContext = services.GetService<ApplicationDbContext>();

    //     // Try Docker path first, then development path
    //     var dockerPath = Path.Combine(AppContext.BaseDirectory, "Data", "RAW", "tsos.csv");
    //     var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;
    //     var devPath = projectRoot != null ? Path.Combine(projectRoot, "Data", "RAW", "tsos.csv") : null;

    //     var filePath = File.Exists(dockerPath) ? dockerPath : devPath;

    //     using var reader = new StreamReader(filePath);
    //     using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    //     csv.Context.RegisterClassMap<TsoCsvMap>();

    //     var records = csv.GetRecords<TsoCsvRecord>().ToList();

    //     var dbIds = await _dbContext.Tsos.Select(t => t.Id).ToListAsync();

    //     if (records.Count() == dbIds.Count)
    //     {
    //         Console.WriteLine("No changes detected. Skipping TSO import.");
    //         return;
    //     }

    //     var dbIdSet = new HashSet<Guid>(dbIds);

    //     Console.WriteLine("Looping TSO records...");

    //     Console.WriteLine(records);

    //     var newRecords = records
    //         .Where(r => !dbIdSet.Contains(r.id))
    //         .Select(r => new TsoEntity
    //         {
    //             Id = r.id,
    //             FqName = r.fqName,
    //             Code = Enum.Parse<TsoCode>(r.code),
    //             ZoneId = Guid.Parse(r.zone_id),
    //             Status = (TsoStatus)Enum.Parse(typeof(TsoStatus), r.status, true)
    //         })
    //         .ToList();

    //     Console.WriteLine("Adding TSOs...");

    //     if (newRecords.Any())
    //     {
    //         await _dbContext.Tsos.AddRangeAsync(newRecords);
    //         await _dbContext.SaveChangesAsync();
    //         Console.WriteLine($"Inserted {newRecords.Count} new TSO records.");
    //     }
    //     else
    //     {
    //         Console.WriteLine("No new TSO records to insert.");
    //     }
    // }
}


// public class TsoCsvMap : ClassMap<TsoCsvRecord>
// {
//     public TsoCsvMap()
//     {
//         Map(m => m.id).Name("id");
//         Map(m => m.fqName).Name("fqName");
//         Map(m => m.code).Name("code");
//         Map(m => m.zone_id).Name("zone_id");
//         Map(m => m.status).Name("status");
//     }
// }

// public class TsoCsvRecord
// {
//     public Guid id { get; set; }
//     public string fqName { get; set; }
//     public string code { get; set; }
//     public string zone_id { get; set; }
//     public string status { get; set; }
// }

#pragma warning restore
