using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Hmm.Idp.Data;
using Hmm.Idp.Pages.Admin.User;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        // Add ASP.NET Identity with SQLite
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
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

        // Configure external authentication providers (Google + GitHub)
        builder.Services.AddAuthentication()
            .AddGoogle("Google", options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.ClientId = configuration["ExternalAuth:Google:ClientId"]!;
                options.ClientSecret = configuration["ExternalAuth:Google:ClientSecret"]!;
            })
            .AddGitHub("GitHub", options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.ClientId = configuration["ExternalAuth:GitHub:ClientId"]!;
                options.ClientSecret = configuration["ExternalAuth:GitHub:ClientSecret"]!;
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
                options.ConfigureDbContext = b => b.UseSqlite(connectionString);
            })
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b => b.UseSqlite(connectionString);
            })
            .AddAspNetIdentity<ApplicationUser>()
            .AddResourceOwnerValidator<CustomResourceOwnerPasswordValidator>();

        // Register user repository (used by login, external login, and user management)
        builder.Services.AddScoped<ApplicationUserRepository>();

        // Register PasswordOptions for PasswordPolicyService
        builder.Services.AddSingleton(new PasswordOptions
        {
            RequireDigit = true,
            RequireLowercase = true,
            RequireNonAlphanumeric = true,
            RequireUppercase = true,
            RequiredLength = 12,
            RequiredUniqueChars = 6
        });

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
        builder.Services.AddScoped<IUserManagementService>(sp => sp.GetRequiredService<UserManagementService>());

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

        // Enable request body buffering so API controllers can read the body
        // even after middleware (e.g. anti-forgery) has consumed the stream
        app.Use(async (context, next) =>
        {
            context.Request.EnableBuffering();
            await next();
        });

        InitializeDatabase(app);
        app.UseStaticFiles();
        app.UseRouting();

        app.UseIdentityServer();

        app.UseAuthorization();
        app.MapControllers();
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
            // SQLite with multiple DbContexts: EnsureCreated() only creates tables when
            // the database file doesn't exist. For subsequent contexts sharing the same
            // database, we must use CreateTables() to add their tables.
            var applicationDbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            applicationDbContext.Database.EnsureCreated();
            Log.Information("ApplicationDbContext database ensured");

            // Enable WAL mode for cloud sync friendliness
            applicationDbContext.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

            var persistedGrantDbContext = serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
            try
            {
                persistedGrantDbContext.Database.EnsureCreated();
                // If database already existed, EnsureCreated is a no-op; try creating tables directly
                var creator = persistedGrantDbContext.GetService<IRelationalDatabaseCreator>();
                creator?.CreateTables();
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                // Tables already exist - safe to ignore
            }
            Log.Information("PersistedGrantDbContext database ensured");

            var configurationDbContext = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            try
            {
                configurationDbContext.Database.EnsureCreated();
                var creator = configurationDbContext.GetService<IRelationalDatabaseCreator>();
                creator?.CreateTables();
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                // Tables already exist - safe to ignore
            }
            Log.Information("ConfigurationDbContext database ensured");

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
                SeedIdentityServerConfiguration(configurationDbContext);

                // Seed Users
                var seedDataService = serviceScope.ServiceProvider.GetRequiredService<SeedDataService>();
                seedDataService.SeedAsync().GetAwaiter().GetResult();
                Log.Information("User seeding completed");
            }

            Log.Information("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while initializing the database");
        }
    }

    private static void SeedIdentityServerConfiguration(ConfigurationDbContext context)
    {
        // Seed Identity Resources
        if (!context.IdentityResources.Any())
        {
            var identityResources = new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email()
            };

            foreach (var resource in identityResources)
            {
                context.IdentityResources.Add(resource.ToEntity());
            }

            context.SaveChanges();
            Log.Information("Seeded IdentityResources: openid, profile, email");
        }

        // Seed API Scopes
        if (!context.ApiScopes.Any(s => s.Name == "hmmapi"))
        {
            var apiScope = new ApiScope("hmmapi", "Hmm API")
            {
                UserClaims = { "name", "email", "role" }
            };

            context.ApiScopes.Add(apiScope.ToEntity());
            context.SaveChanges();
            Log.Information("Seeded ApiScope: hmmapi");
        }

        // Seed API Resources
        if (!context.ApiResources.Any(r => r.Name == "hmmapi"))
        {
            var apiResource = new ApiResource("hmmapi", "Hmm API")
            {
                Scopes = { "hmmapi" },
                UserClaims = { "name", "email", "role" }
            };

            context.ApiResources.Add(apiResource.ToEntity());
            context.SaveChanges();
            Log.Information("Seeded ApiResource: hmmapi");
        }

        // Seed Clients
        SeedClients(context);

        Log.Information("IdentityServer configuration seeding completed");
    }

    private static void SeedClients(ConfigurationDbContext context)
    {
        // hmm.functest - Resource Owner Password Grant
        if (!context.Clients.Any(c => c.ClientId == "hmm.functest"))
        {
            var funcTestClient = new Client
            {
                ClientId = "hmm.functest",
                ClientName = "Hmm Functional Testing Client",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                ClientSecrets = { new Secret("FuncTestSecret123!".Sha256()) },
                AllowedScopes = { "openid", "profile", "email", "hmmapi" },
                AllowOfflineAccess = true,
                AccessTokenLifetime = 3600,
                SlidingRefreshTokenLifetime = 86400,
                AbsoluteRefreshTokenLifetime = 2592000,
                RefreshTokenExpiration = TokenExpiration.Sliding
            };

            context.Clients.Add(funcTestClient.ToEntity());
            context.SaveChanges();
            Log.Information("Seeded Client: hmm.functest");
        }

        // hmm.m2m - Client Credentials Grant
        if (!context.Clients.Any(c => c.ClientId == "hmm.m2m"))
        {
            var m2mClient = new Client
            {
                ClientId = "hmm.m2m",
                ClientName = "Hmm Machine-to-Machine Client",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("M2MSecret456!".Sha256()) },
                AllowedScopes = { "hmmapi" }
            };

            context.Clients.Add(m2mClient.ToEntity());
            context.SaveChanges();
            Log.Information("Seeded Client: hmm.m2m");
        }

        // hmm.web - Authorization Code with PKCE
        if (!context.Clients.Any(c => c.ClientId == "hmm.web"))
        {
            var webClient = new Client
            {
                ClientId = "hmm.web",
                ClientName = "Hmm Web Application",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                ClientSecrets = { new Secret("WebSecret789!".Sha256()) },
                AllowedScopes = { "openid", "profile", "email", "hmmapi" },
                AllowOfflineAccess = true,
                UpdateAccessTokenClaimsOnRefresh = true,
                RedirectUris =
                {
                    "https://localhost:5002/signin-oidc",
                    "https://localhost:44342/signin-oidc"
                },
                PostLogoutRedirectUris =
                {
                    "https://localhost:5002/signout-callback-oidc",
                    "https://localhost:44342/signout-callback-oidc"
                }
            };

            context.Clients.Add(webClient.ToEntity());
            context.SaveChanges();
            Log.Information("Seeded Client: hmm.web");
        }

        // hmm.serviceapi - Client Credentials + Token Introspection
        if (!context.Clients.Any(c => c.ClientId == "hmm.serviceapi"))
        {
            var serviceApiClient = new Client
            {
                ClientId = "hmm.serviceapi",
                ClientName = "Hmm Service API",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("ServiceApiSecret!@#456".Sha256()) },
                AllowedScopes = { "hmmapi" },
                Properties = { { "AllowTokenIntrospection", "true" } }
            };

            context.Clients.Add(serviceApiClient.ToEntity());
            context.SaveChanges();
            Log.Information("Seeded Client: hmm.serviceapi");
        }
    }
}
