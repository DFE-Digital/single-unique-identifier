using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using SUI.StubCustodians.API.OpenApi;
using SUI.StubCustodians.API.OpenApiTransformers;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Services;
using SUI.StubCustodians.Infrastructure.Extensions;
using SUI.StubCustodians.Infrastructure.Services;

namespace SUI.StubCustodians.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.UseOpenTelemetry();

            builder.Services.AddOpenApi(options =>
            {
                options.AddSchemaTransformer<CustodiansOpenApiSchemaTransformer>();
                options.AddDocumentTransformer<CustodiansOpenApiDocumentTransformer>();
                options.AddDocumentTransformer<FindDocumentFilter>();
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
            app.UseMiddleware<ScopeEnforcementMiddleware>();
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
            services.AddSingleton<IRandomDelayService>(_ => new RandomDelayService(3, 10));
            services.AddSingleton<IDataProvider, FileDataProvider>();
            services.AddScoped<IManifestService, ManifestService>();
            services.AddScoped<IRecordService, RecordService>();
        }
    }
}
