using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace SUI.Find.FindApi;

public class CustomOpenApiHttpTrigger(ILogger<CustomOpenApiHttpTrigger> logger)
{

  [Function("RenderSwaggerDocument")]
  public async Task<HttpResponseData> RenderSwaggerDocument(
      [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger.json")] HttpRequestData req)
  {
    logger.LogInformation("Serving custom OpenAPI document");

    var response = req.CreateResponse(HttpStatusCode.OK);
    response.Headers.Add("Content-Type", "application/x-yaml");

    var openApiYaml = await File.ReadAllTextAsync("openapi_v1.yaml");
    await response.WriteStringAsync(openApiYaml);

    return response;
  }
}
