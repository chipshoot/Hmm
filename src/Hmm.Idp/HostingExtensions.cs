using Duende.IdentityServer.EntityFramework.DbContexts;
using Hmm.Idp.Data;
using Hmm.Idp.Pages.Admin.User;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using IServiceScopeFactory = Microsoft.Extensions.DependencyInjection.IServiceScopeFactory;

namespace Hmm.Idp;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddRazorPages();

        var migrationsAssembly = typeof(Program).Assembly.GetName().Name;

        // Add ASP.NET Identity
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Configure Identity options
        builder.Services.Configure<IdentityOptions>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 12;
            options.Password.RequiredUniqueChars = 6;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            // Sign-in settings
            options.SignIn.RequireConfirmedEmail = true;
            options.SignIn.RequireConfirmedAccount = true;
        });

        // Configure email settings
        builder.Services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        builder.Services.AddIdentityServer()
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = b => b.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b => b.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddAspNetIdentity<ApplicationUser>();

        // Register credential management services
        builder.Services.AddScoped<PasswordPolicyService>();
        builder.Services.AddScoped<AccountLockoutService>();
        builder.Services.AddScoped<TwoFactorAuthService>();
        builder.Services.AddScoped<PasswordResetService>();
        builder.Services.AddScoped<IEmailService, EmailService>();

        // Add this to ConfigureServices method in HostingExtensions.cs
        builder.Services.AddScoped<ApiResourceService>();

        // Add the user management service
        builder.Services.AddScoped<UserManagementService>();

        // Add seed data service
        builder.Services.AddScoped<SeedDataService>();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        InitializeDatabase(app);
        app.UseStaticFiles();
        app.UseRouting();

        app.UseIdentityServer();

        app.UseAuthorization();
        app.MapRazorPages().RequireAuthorization();

        return app;
    }

    private static void InitializeDatabase(IApplicationBuilder builder)
    {
        var scopeFactory = builder.ApplicationServices.GetService<IServiceScopeFactory>();
        if (scopeFactory == null)
        {
            return;
        }

        using var serviceScope = scopeFactory.CreateScope();

        try
        {
            // Migrate ASP.NET Identity Database
            var applicationDbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            applicationDbContext.Database.Migrate();

            var persistedGrantDbContext = serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
            persistedGrantDbContext.Database.Migrate();

            var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            context.Database.Migrate();

            // Check if we should seed data (Development or Docker environment)
            var shouldSeed = false;
            if (builder is WebApplication app)
            {
                shouldSeed = app.Environment.IsDevelopment() ||
                             app.Environment.EnvironmentName == "Docker" ||
                             Environment.GetEnvironmentVariable("SEED_DATA") == "true";
            }

            if (shouldSeed)
            {
                // Seed Users (IdentityServer config data is seeded via SQL script in init-db.sql)
                var seedDataService = serviceScope.ServiceProvider.GetRequiredService<SeedDataService>();
                seedDataService.SeedAsync().GetAwaiter().GetResult();
                Log.Information("User seeding completed");
            }
        }
        catch (Exception ex)
        {
            // Log the exception but allow the application to continue
            Log.Error(ex, "An error occurred while initializing the database");
        }
    }
}