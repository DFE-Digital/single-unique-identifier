using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Contracts.Arbor;
using SUI.StubCustodians.Application.Contracts.Mosaic;
using SUI.StubCustodians.Application.Contracts.Niche;
using SUI.StubCustodians.Application.Contracts.SystmOne;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Mappers;
using SUI.StubCustodians.Application.Queries;
using SUI.StubCustodians.Application.Services;

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
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssemblyContaining<GetEventRecordBySuiQuery>();
            });

            services.AddScoped<
                IRecordProvider<PersonalDetailsRecordV1>,
                PersonalDetailsRecordProvider
            >();

            services.AddScoped<
                IRecordProvider<EducationDetailsRecordV1>,
                EducationDetailsRecordProvider
            >();

            string activeCustodian = configuration.GetValue<string>(
                "ActiveCustodian",
                "MockEducationProvider"
            );

            if (activeCustodian.Equals("Arbor", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<IEventRecordProvider, ArborEventRecordProvider>();
                services.AddScoped<IRecordMapper<ArborRecord>, ArborRecordMapper>();
            }
            else if (activeCustodian.Equals("Mosaic", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<IEventRecordProvider, MosaicEventRecordProvider>();
                services.AddScoped<IRecordMapper<MosaicRecord>, MosaicRecordMapper>();
            }
            else if (activeCustodian.Equals("SystmOne", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<IEventRecordProvider, SystmOneEventRecordProvider>();
                services.AddScoped<IRecordMapper<SystmOneRecord>, SystmOneRecordMapper>();
            }
            else if (activeCustodian.Equals("Niche", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<IEventRecordProvider, NicheEventRecordProvider>();
                services.AddScoped<IRecordMapper<NicheRecord>, NicheRecordMapper>();
            }
            else
            {
                throw new InvalidOperationException($"Unknown Custodian: {activeCustodian}");
            }
        }
    }
}
