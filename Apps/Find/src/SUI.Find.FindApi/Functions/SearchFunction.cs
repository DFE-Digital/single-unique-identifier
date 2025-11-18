using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.Functions;

public class SearchFunction(ILogger<SearchFunction> logger)
{
  [Function(nameof(Searches))]
  public async Task<HttpStatusCode> Searches([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "/v1/searches")] HttpRequestData req)
  {
    var request = await JsonSerializer.DeserializeAsync<StartSearchRequest>(req.Body);
    logger.LogInformation("Requesting Search with Id: {Suid}", request?.Suid);
    return HttpStatusCode.Accepted;
  }
}


