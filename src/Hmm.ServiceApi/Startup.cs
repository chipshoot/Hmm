using Hmm.Core;
using Hmm.Core.Dal.EF;
using Hmm.Core.Dal.EF.Repositories;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.Configuration;
using Hmm.ServiceApi.DtoEntity;
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
using Microsoft.Net.Http.Headers;
using Npgsql;
using System;
using System.IO;

namespace Hmm.ServiceApi
{
    public class Startup(IConfiguration configuration)
    {
        private const string AllowCorsPolicy = "AllowCors";
        private InvalidModelStateConfig _invalidModelStateConfig;

        private IConfiguration Configuration { get; } = configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services, IWebHostEnvironment env)
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
            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", opt =>
                {
                    opt.Authority = appSetting.IdpBaseUrl;
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidAudience = appSetting.ApiAudience
                    };
                });
            services.AddApiVersioning(setup =>
            {
                setup.DefaultApiVersion = new ApiVersion(1, 0);
                setup.AssumeDefaultVersionWhenUnspecified = true;
                setup.ReportApiVersions = true;
            });
            services.AddHttpClient(HmmServiceApiConstants.HttpClient.Idp, client =>
            {
                client.BaseAddress = new Uri(appSetting.IdpBaseUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            });
            services.AddHttpContextAccessor();
            services.AddCors(opt =>
            {
                opt.AddPolicy(AllowCorsPolicy, builder =>
                {
                    builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
                });
            });

            // Configure NpgsqlDataSource with enum mapping
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.MapEnum<Hmm.Core.Map.DbEntity.NoteContentFormatType>();
            var dataSource = dataSourceBuilder.Build();

            services
                .AddDbContext<HmmDataContext>(opt =>
                {
                    opt.UseNpgsql(dataSource);
                    if (env.IsDevelopment())
                    {
                        opt.EnableSensitiveDataLogging();
                    }
                })
                .AddSingleton<IDateTimeProvider, DateTimeAdapter>()
                .AddScoped<IHmmDataContext, HmmDataContext>()
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
                .AddScoped<IHmmValidator<Author>, AuthorValidator>()
                .AddScoped<IHmmValidator<NoteCatalog>, NoteCatalogValidator>()
                .AddScoped<IHmmValidator<HmmNote>, NoteValidator>()
                .AddScoped<IHmmValidator<Tag>, TagValidator>()
                .AddScoped<IHmmValidator<Contact>, ContactValidator>()
                //.AddTransient<IPropertyMappingService, PropertyMappingService>()
                .AddTransient<IPropertyCheckService, PropertyCheckService>()
                .AddAutoMapper(cfg =>
                {
                    cfg.AddProfile<ApiMappingProfile>();
                    cfg.AddProfile<HmmMappingProfile>();
                })
                .AddSwaggerGen();
            services.AddExceptionHandler<GlobalExceptionHandler>();

            //var automobileStartup = new AutomobileInfoServiceStartup(services);
            //automobileStartup.ConfigureServices();
            //services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler(_ => { });

            if (env.IsDevelopment())
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
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

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
    }
}