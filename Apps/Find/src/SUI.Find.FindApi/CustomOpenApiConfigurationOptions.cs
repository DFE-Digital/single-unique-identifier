using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace SUI.Find.FindApi;

public class CustomOpenApiConfigurationOptions : DefaultOpenApiConfigurationOptions
{
  public override OpenApiInfo Info { get; set; } = new OpenApiInfo
  {
    Version = "1.0.0",
    Title = "TestDurableFunc API",
    Description = "Azure Functions API"
  };

  public override OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;
}
