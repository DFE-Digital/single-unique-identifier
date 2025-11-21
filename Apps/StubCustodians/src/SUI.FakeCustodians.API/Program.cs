using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using SUI.FakeCustodians.Application.Contracts.Arbor;
using SUI.FakeCustodians.Application.Contracts.Mosaic;
using SUI.FakeCustodians.Application.Contracts.Niche;
using SUI.FakeCustodians.Application.Contracts.SystmOne;
using SUI.FakeCustodians.Application.Interfaces;
using SUI.FakeCustodians.Application.Mappers;
using SUI.FakeCustodians.Application.Queries;
using SUI.FakeCustodians.Application.Services;

namespace SUI.FakeCustodians.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

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
                var apiVersionDescriptionProvider =
                    app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    foreach (
                        var description in apiVersionDescriptionProvider.ApiVersionDescriptions
                    )
                    {
                        options.SwaggerEndpoint(
                            $"/swagger/{description.GroupName}/swagger.json",
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

            string activeCustodian = configuration.GetValue<string>("ActiveCustodian", "Mosaic");

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
