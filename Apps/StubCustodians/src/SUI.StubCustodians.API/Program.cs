using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.API.OpenApiTransformers;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Services;
using SUI.StubCustodians.Infrastructure.Services;

namespace SUI.StubCustodians.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddOpenApi(options =>
            {
                options.AddSchemaTransformer<CustodiansOpenApiSchemaTransformer>();
                options.AddDocumentTransformer<CustodiansOpenApiDocumentTransformer>();
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddProblemDetails();
            builder.Services.AddHttpContextAccessor();

            builder
                .Services.AddApiVersioning(opt =>
                {
                    opt.DefaultApiVersion = new ApiVersion(1, 0);
                    opt.AssumeDefaultVersionWhenUnspecified = true;
                    opt.ReportApiVersions = true;
                    opt.ApiVersionReader = ApiVersionReader.Combine(
                        new UrlSegmentApiVersionReader(),
                        new HeaderApiVersionReader("x-api-version"),
                        new MediaTypeApiVersionReader("x-api-version")
                    );
                })
                .AddApiExplorer(setup =>
                {
                    setup.GroupNameFormat = "'v'VVV";
                    setup.SubstituteApiVersionInUrl = true;
                });

            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

                app.MapOpenApi("/openapi/v1.json");

                app.UseSwaggerUI(options =>
                {
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerEndpoint(
                            $"/openapi/{description.GroupName}.json",
                            description.GroupName.ToUpperInvariant()
                        );
                    }
                });
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }

        private static void ConfigureServices(
            IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.AddScoped(typeof(IRecordServiceHandler<>), typeof(RecordServiceHandler<>));

            services.AddScoped<
                IRecordProvider<PersonalDetailsRecord>,
                PersonalDetailsRecordProvider
            >();

            services.AddScoped<
                IRecordProvider<EducationDetailsRecord>,
                EducationDetailsRecordProvider
            >();

            services.AddScoped<
                IRecordProvider<ChildrensServicesDetailsRecord>,
                ChildrensServicesDetailsRecordProvider
            >();

            services.AddScoped<IRecordProvider<CrimeDataRecord>, CrimeDataRecordProvider>();

            services.AddScoped<IRecordProvider<HealthDataRecord>, HealthDataRecordProvider>();

            services.AddSingleton<IRandomDelayService>(_ => new RandomDelayService(3, 10));
            services.AddSingleton<IDataProvider, FileDataProvider>();
            services.AddScoped<IManifestService, ManifestService>();
            services.AddScoped<IRecordService, RecordService>();
        }
    }
}
