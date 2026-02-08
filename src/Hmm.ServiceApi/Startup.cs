using Hmm.Core;
using Hmm.Core.Dal.EF;
using Hmm.Core.Dal.EF.Repositories;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure;
using Hmm.ServiceApi.Configuration;
using Hmm.ServiceApi.DtoEntity.Profiles;
using Hmm.ServiceApi.DtoEntity.Services;
using Hmm.ServiceApi.Middleware;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Hmm.ServiceApi
{
    public class Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        private const string AllowCorsPolicy = "AllowCors";
        private InvalidModelStateConfig _invalidModelStateConfig;

        private IConfiguration Configuration { get; } = configuration;
        private IWebHostEnvironment Environment { get; } = env;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var configSection = SetupConfiguration(services);
            var appSetting = configSection.Get<AppSettings>();
            services.AddControllers(setupAction =>
                        {
                            setupAction.ReturnHttpNotAcceptable = true;
                        })
                            .AddJsonOptions(opt => opt.JsonSerializerOptions.PropertyNamingPolicy = null)
                            .AddNewtonsoftJson()
                            //.AddNewtonsoftJson(setupAction =>
                            //{
                            //    setupAction.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                            //})
                            .AddXmlDataContractSerializerFormatters()
                            .ConfigureApiBehaviorOptions(setupAction =>
                            {
                                setupAction.InvalidModelStateResponseFactory = context =>
                                {
                                    var problemDetails = new ValidationProblemDetails(context.ModelState)
                                    {
                                        Type = _invalidModelStateConfig.Type,
                                        Title = _invalidModelStateConfig.Title,
                                        Status = StatusCodes.Status422UnprocessableEntity,
                                        Detail = _invalidModelStateConfig.Detail,
                                        Instance = context.HttpContext.Request.Path
                                    };
                                    problemDetails.Extensions.Add("traceId", context.HttpContext.TraceIdentifier);

                                    return new UnprocessableEntityObjectResult(problemDetails)
                                    {
                                        ContentTypes = { "application/problem+json" }
                                    };
                                };
                            });
            ConfigureAuthentication(services, appSetting);
            services.AddApiVersioning(setup =>
            {
                setup.DefaultApiVersion = new ApiVersion(1, 0);
                setup.AssumeDefaultVersionWhenUnspecified = true;
                setup.ReportApiVersions = true;
            });
            services.AddHttpContextAccessor();
            services.AddCors(opt =>
            {
                opt.AddPolicy(AllowCorsPolicy, builder =>
                {
                    if (appSetting.CorsOrigins != null && appSetting.CorsOrigins.Length > 0)
                    {
                        builder.WithOrigins(appSetting.CorsOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    }
                    else
                    {
                        // Fallback for development if no origins configured - restrict to localhost only
                        builder.WithOrigins("https://localhost:5001", "https://localhost:5002", "http://localhost:3000")
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    }
                });
            });

            // Configure database provider based on AppSettings.DatabaseProvider
            // Default is SQL Server; set to "PostgreSQL" or "SQLite" for alternatives
            var connectionString = Configuration.GetConnectionString("HmmNoteConnection")
                                   ?? Configuration.GetConnectionString("DefaultConnection");
            var databaseProvider = appSetting?.DatabaseProvider ?? "SqlServer";

            services.AddDbContext<HmmDataContext>(opt =>
                {
                    if (string.Equals(databaseProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
                    {
                        // PostgreSQL with Npgsql
                        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
                        dataSourceBuilder.MapEnum<Core.Map.DbEntity.NoteContentFormatType>();
                        var dataSource = dataSourceBuilder.Build();
                        opt.UseNpgsql(dataSource);
                    }
                    else if (string.Equals(databaseProvider, "SQLite", StringComparison.OrdinalIgnoreCase))
                    {
                        opt.UseSqlite(connectionString);
                    }
                    else
                    {
                        // SQL Server (default)
                        opt.UseSqlServer(connectionString);
                    }

                    if (Environment.IsDevelopment())
                    {
                        opt.EnableSensitiveDataLogging();
                    }
                });

            services
                .AddSingleton<IDateTimeProvider, DateTimeAdapter>()
                .AddScoped<IHmmDataContext, HmmDataContext>()
                .AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IHmmDataContext>())
                .AddScoped<IVersionRepository<HmmNoteDao>, NoteEfRepository>()
                .AddScoped<ICompositeEntityRepository<TagDao, HmmNoteDao>, TagEfRepository>()
                .AddScoped<IEntityLookup, EfEntityLookup>()
                .AddScoped<IRepository<AuthorDao>, AuthorEfRepository>()
                .AddScoped<IRepository<ContactDao>, ContactEfRepository>()
                .AddScoped<IRepository<NoteCatalogDao>, NoteCatalogEfRepository>()
                .AddScoped<IAuthorManager, AuthorManager>()
                .AddScoped<IContactManager, ContactManager>()
                .AddScoped<IHmmNoteManager, HmmNoteManager>()
                .AddScoped<INoteCatalogManager, NoteCatalogManager>()
                .AddScoped<ITagManager, TagManager>()
                .AddScoped<INoteTagAssociationManager, NoteTagAssociationManager>()
                // Validators registered as Transient for thread-safety:
                // - Each validation operation gets a fresh validator instance
                // - Prevents any potential state leakage between concurrent validations
                // - FluentValidation's async rules are safest with transient lifetime
                .AddTransient<IHmmValidator<Author>, AuthorValidator>()
                .AddTransient<IHmmValidator<NoteCatalog>, NoteCatalogValidator>()
                .AddTransient<IHmmValidator<HmmNote>, NoteValidator>()
                .AddTransient<IHmmValidator<Tag>, TagValidator>()
                .AddTransient<IHmmValidator<Contact>, ContactValidator>()
                .AddTransient<IPropertyMappingService, PropertyMappingService>()
                .AddTransient<IPropertyCheckService, PropertyCheckService>()
                .AddAutoMapper(cfg =>
                {
                    cfg.AddProfile<ApiMappingProfile>();
                    cfg.AddProfile<HmmMappingProfile>();
                })
                .AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "HomeMadeMessage API",
                        Version = "v1",
                        Description = "RESTful API for HomeMadeMessage - Note management, automobile tracking, and calendar services.",
                        Contact = new OpenApiContact
                        {
                            Name = "HomeMadeMessage Support",
                            Email = "support@homemademessage.com"
                        },
                        License = new OpenApiLicense
                        {
                            Name = "MIT License"
                        }
                    });

                    // Add JWT Bearer authentication
                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    });

                    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                    });

                    // Include XML comments for API documentation
                    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
                    if (File.Exists(xmlPath))
                    {
                        options.IncludeXmlComments(xmlPath);
                    }
                });
            services.AddExceptionHandler<GlobalExceptionHandler>();

            // Register Automobile module services (managers, validators, serializers)
            var automobileStartup = new AutomobileInfoServiceStartup(services);
            automobileStartup.ConfigureServices();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler(_ => { });

            // SQLite: auto-create database and enable WAL mode for cloud sync friendliness
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<HmmDataContext>();
                if (dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
                {
                    dbContext.Database.EnsureCreated();
                    dbContext.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
                }
            }

            if (env.IsDevelopment() || env.EnvironmentName == "Docker")
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hmm.ServiceApi v1"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private IConfigurationSection SetupConfiguration(IServiceCollection services)
        {
            var environmentName = Environment.EnvironmentName;

            _invalidModelStateConfig = new InvalidModelStateConfig();
            Configuration.Bind("InvalidModelState", _invalidModelStateConfig);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{environmentName}.json", true)
                .AddEnvironmentVariables()
                .Build();

            var appSettingsSection = configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            return appSettingsSection;
        }

        /// <summary>
        /// Configures authentication with support for multiple identity providers.
        /// Supports: Hmm.Idp (internal), Firebase, Auth0, and Azure AD.
        /// </summary>
        private static void ConfigureAuthentication(IServiceCollection services, AppSettings appSetting)
        {
            var externalAuth = appSetting.ExternalAuth;
            var hasExternalProviders = externalAuth != null &&
                (externalAuth.EnableFirebase || externalAuth.EnableAuth0 || externalAuth.EnableAzureAd);

            // Allow HTTP for Docker/Development when IdpBaseUrl uses http://
        var requireHttpsMetadata = !appSetting.IdpBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);

            if (!hasExternalProviders)
            {
                // Simple single-provider configuration (backward compatible)
                services.AddAuthentication("Bearer")
                    .AddJwtBearer("Bearer", opt =>
                    {
                        opt.Authority = appSetting.IdpBaseUrl;
                        opt.RequireHttpsMetadata = requireHttpsMetadata;
                        opt.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateAudience = true,
                            ValidAudience = appSetting.ApiAudience
                        };
                    });
                return;
            }

            // Multi-provider configuration
            var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = MultiAuthSchemeSelector.MultiAuthScheme;
                options.DefaultChallengeScheme = MultiAuthSchemeSelector.MultiAuthScheme;
            });

            // Always add the internal Hmm.Idp scheme
            authBuilder.AddJwtBearer(MultiAuthSchemeSelector.HmmIdpScheme, opt =>
            {
                opt.Authority = appSetting.IdpBaseUrl;
                opt.RequireHttpsMetadata = requireHttpsMetadata;
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = appSetting.ApiAudience,
                    ValidateIssuer = true,
                    ValidateLifetime = true
                };
            });

            // Add Firebase authentication if enabled
            if (externalAuth.EnableFirebase && !string.IsNullOrEmpty(externalAuth.FirebaseProjectId))
            {
                authBuilder.AddJwtBearer(MultiAuthSchemeSelector.FirebaseScheme, opt =>
                {
                    var projectId = externalAuth.FirebaseProjectId;
                    opt.Authority = $"https://securetoken.google.com/{projectId}";
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = $"https://securetoken.google.com/{projectId}",
                        ValidateAudience = true,
                        ValidAudience = projectId,
                        ValidateLifetime = true
                    };
                });
            }

            // Add Auth0 authentication if enabled
            if (externalAuth.EnableAuth0 &&
                !string.IsNullOrEmpty(externalAuth.Auth0Domain) &&
                !string.IsNullOrEmpty(externalAuth.Auth0Audience))
            {
                authBuilder.AddJwtBearer(MultiAuthSchemeSelector.Auth0Scheme, opt =>
                {
                    opt.Authority = $"https://{externalAuth.Auth0Domain}/";
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = $"https://{externalAuth.Auth0Domain}/",
                        ValidateAudience = true,
                        ValidAudience = externalAuth.Auth0Audience,
                        ValidateLifetime = true
                    };
                });
            }

            // Add Azure AD authentication if enabled
            if (externalAuth.EnableAzureAd &&
                !string.IsNullOrEmpty(externalAuth.AzureAdTenantId) &&
                !string.IsNullOrEmpty(externalAuth.AzureAdClientId))
            {
                authBuilder.AddJwtBearer(MultiAuthSchemeSelector.AzureAdScheme, opt =>
                {
                    opt.Authority = $"https://login.microsoftonline.com/{externalAuth.AzureAdTenantId}/v2.0";
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = $"https://login.microsoftonline.com/{externalAuth.AzureAdTenantId}/v2.0",
                        ValidateAudience = true,
                        ValidAudience = externalAuth.AzureAdClientId,
                        ValidateLifetime = true
                    };
                });
            }

            // Add the policy scheme that selects the appropriate auth scheme based on token
            authBuilder.AddPolicyScheme(MultiAuthSchemeSelector.MultiAuthScheme, "Multi-provider Authentication", options =>
            {
                options.ForwardDefaultSelector = context =>
                    MultiAuthSchemeSelector.SelectScheme(context, externalAuth);
            });
        }
    }
}