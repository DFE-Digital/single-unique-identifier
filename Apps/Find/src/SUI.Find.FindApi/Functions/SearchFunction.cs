using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Validators;

namespace SUI.Find.FindApi.Functions;

public class SearchFunction(ILogger<SearchFunction> logger)
{
    [Function(nameof(Searches))]
    public async Task<HttpResponseData> Searches(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/searches")]
            HttpRequestData req
    )
    {
        var searchRequest = await JsonSerializer.DeserializeAsync<StartSearchRequest>(req.Body);

        if (!StartSearchRequestValidator.IsValid(searchRequest, out var errorMessage))
        {
            logger.LogWarning(
                "[Validation] Invalid Search Request Received: {ErrorMessage}",
                errorMessage
            );
            var problem = new Problem(
                Type: nameof(StartSearchRequest),
                Title: "Invalid Search Request",
                Detail: errorMessage,
                Status: (int)HttpStatusCode.BadRequest,
                Instance: null
            );

            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(problem);

            return response;
        }

        logger.LogInformation("Requesting Search with Id: {Suid}", searchRequest?.Suid);

        var acceptedResponse = req.CreateResponse(HttpStatusCode.Accepted);
        return acceptedResponse;
    }
}
