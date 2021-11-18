using Hmm.Core;
using Hmm.Core.Dal.EF;
using Hmm.Core.Dal.EF.Repositories;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.ServiceApi.Configuration;
using Hmm.ServiceApi.DtoEntity;
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
using Microsoft.Net.Http.Headers;
using System;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure;

namespace Hmm.ServiceApi
{
    public class Startup
    {
        private string _connectString;
        private string _idpBaseAddress;
        private InvalidModelStateConfig _invalidModelStateConfig;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            GetConfiguration();
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

            services.AddHttpClient(HmmServiceApiConstants.HttpClient.Idp, client =>
            {
                client.BaseAddress = new Uri(_idpBaseAddress);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            });
            services.AddHttpContextAccessor();
            services
                .AddDbContext<HmmDataContext>(opt => opt.UseSqlServer(_connectString))
                .AddSingleton<IDateTimeProvider, DateTimeAdapter>()
                .AddScoped<IVersionRepository<HmmNote>, NoteEfRepository>()
                .AddScoped<IHmmDataContext, HmmDataContext>()
                .AddScoped<IEntityLookup, EfEntityLookup>()
                .AddScoped<IGuidRepository<Author>, AuthorEfRepository>()
                .AddScoped<IRepository<NoteRender>, NoteRenderEfRepository>()
                .AddScoped<IRepository<NoteCatalog>, NoteCatalogEfRepository>()
                .AddScoped<IRepository<Subsystem>, SubsystemEfRepository>()
                .AddScoped<IAuthorManager, AuthorManager>()
                .AddScoped<IHmmNoteManager, HmmNoteManager>()
                .AddScoped<INoteRenderManager, NoteRenderManager>()
                .AddScoped<INoteCatalogManager, NoteCatalogManager>()
                .AddScoped<ISubsystemManager, SubsystemManager>()
                .AddScoped<IHmmValidator<Author>, AuthorValidator>()
                .AddScoped<IHmmValidator<NoteCatalog>, NoteCatalogValidator>()
                .AddScoped<IHmmValidator<NoteRender>, NoteRenderValidator>()
                .AddScoped<IHmmValidator<Subsystem>, SubsystemValidator>()
                .AddScoped<IHmmValidator<HmmNote>, NoteValidator>()
                .AddAutoMapper(typeof(ApiEntity))
                .AddSwaggerGen();


            var automobileStartup = new AutomobileInfoServiceStartup(services);
            automobileStartup.ConfigureServices();
            //services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hmm.ServiceApi v1"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void GetConfiguration()
        {
            _connectString = Configuration?.GetConnectionString("DefaultConnection");
            _idpBaseAddress = Configuration?["IdentityService"];

            _invalidModelStateConfig = new InvalidModelStateConfig();
            Configuration.Bind("InvalidModelState", _invalidModelStateConfig);
        }
    }
}