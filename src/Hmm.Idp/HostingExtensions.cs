using Duende.IdentityServer.EntityFramework.DbContexts;
using Hmm.Idp.Data;
using Hmm.Idp.Pages.Admin.User;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
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

        var identityServerBuilder = builder.Services.AddIdentityServer(options =>
            {
                // Set a fixed issuer URI for consistent token validation across environments
                // In Docker, this ensures tokens issued via localhost:5001 are also valid for internal hmm-idp:80 validation
                var issuerUri = configuration.GetValue<string>("IssuerUri");
                if (!string.IsNullOrEmpty(issuerUri))
                {
                    options.IssuerUri = issuerUri;
                }
            })
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

        // Retry logic for transient database connection failures
        const int maxRetries = 10;
        const int delayMs = 3000;

        for (var retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                var applicationDbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Create database if it doesn't exist (without creating tables - migrations will do that)
                var connectionString = applicationDbContext.Database.GetConnectionString();
                EnsureDatabaseExists(connectionString!);

                // Run migrations for all databases
                applicationDbContext.Database.Migrate();
                Log.Information("ApplicationDbContext migrations applied");

                var persistedGrantDbContext = serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
                persistedGrantDbContext.Database.Migrate();
                Log.Information("PersistedGrantDbContext migrations applied");

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                context.Database.Migrate();
                Log.Information("ConfigurationDbContext migrations applied");

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
                    // Seed IdentityServer configuration (clients, scopes, resources)
                    SeedIdentityServerConfiguration(connectionString!);

                    // Seed Users
                    var seedDataService = serviceScope.ServiceProvider.GetRequiredService<SeedDataService>();
                    seedDataService.SeedAsync().GetAwaiter().GetResult();
                    Log.Information("User seeding completed");
                }

                Log.Information("Database initialization completed successfully");
                return;
            }
            catch (Exception ex) when (retry < maxRetries - 1)
            {
                Log.Warning(ex, "Database initialization attempt {Attempt} of {MaxRetries} failed, retrying in {DelayMs}ms...",
                    retry + 1, maxRetries, delayMs);
                Thread.Sleep(delayMs);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while initializing the database after {MaxRetries} attempts", maxRetries);
            }
        }
    }

    private static void EnsureDatabaseExists(string connectionString)
    {
        // Parse the connection string to extract the database name
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        // Connect to master database to create the target database
        builder.InitialCatalog = "master";

        using var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();

        // Check if database exists
        using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = $"SELECT COUNT(*) FROM sys.databases WHERE name = @dbName";
        checkCommand.Parameters.AddWithValue("@dbName", databaseName);

        var exists = (int)checkCommand.ExecuteScalar()! > 0;

        if (!exists)
        {
            Log.Information("Creating database {DatabaseName}", databaseName);
            using var createCommand = connection.CreateCommand();
            createCommand.CommandText = $"CREATE DATABASE [{databaseName}]";
            createCommand.ExecuteNonQuery();
            Log.Information("Database {DatabaseName} created successfully", databaseName);
        }
    }

    private static void SeedIdentityServerConfiguration(string connectionString)
    {
        // Read the seed SQL script - check multiple locations
        // In Docker: /app/scripts/init-db.sql
        // In development: DockerFiles/scripts/init-db.sql
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "scripts", "init-db.sql"),           // Docker path
            Path.Combine(AppContext.BaseDirectory, "DockerFiles", "scripts", "init-db.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "scripts", "init-db.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "DockerFiles", "scripts", "init-db.sql")
        };

        var scriptPath = possiblePaths.FirstOrDefault(File.Exists);

        if (scriptPath == null)
        {
            Log.Warning("IdentityServer seed script not found in any expected location, skipping configuration seeding");
            return;
        }

        Log.Information("Loading IdentityServer seed script from {ScriptPath}", scriptPath);
        var fullScript = File.ReadAllText(scriptPath);

        // Extract only the seeding part (skip the database creation section)
        // The seed data starts after "-- Seed IdentityServer Configuration Data"
        var seedMarker = "-- Seed IdentityServer Configuration Data";
        var seedStartIndex = fullScript.IndexOf(seedMarker, StringComparison.Ordinal);

        if (seedStartIndex < 0)
        {
            Log.Warning("Could not find seed data marker in init-db.sql, skipping configuration seeding");
            return;
        }

        var seedScript = fullScript[seedStartIndex..];

        // Split by GO statements and execute each batch
        var batches = seedScript.Split(new[] { "\nGO\n", "\nGO\r\n", "\r\nGO\r\n", "\r\nGO\n" },
            StringSplitOptions.RemoveEmptyEntries);

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        foreach (var batch in batches)
        {
            var trimmedBatch = batch.Trim();
            if (string.IsNullOrWhiteSpace(trimmedBatch) || trimmedBatch == "GO")
                continue;

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = trimmedBatch;
                command.ExecuteNonQuery();
            }
            catch (SqlException ex) when (ex.Number == 2627) // Duplicate key - already seeded
            {
                // Ignore duplicate key errors, data already exists
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error executing seed batch: {Batch}", trimmedBatch[..Math.Min(100, trimmedBatch.Length)]);
            }
        }

        Log.Information("IdentityServer configuration seeding completed");
    }
}