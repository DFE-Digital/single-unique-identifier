using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Functions.Orchestrators;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Validators;

namespace SUI.Find.FindApi.Functions.HttpTriggers;

public class SearchFunction(ILogger<SearchFunction> logger)
{
    [OpenApiOperation(
        operationId: "searches",
        tags: ["Search"],
        Summary = "Submit a new search request"
    )]
    [OpenApiRequestBody(
        contentType: "application/json",
        bodyType: typeof(StartSearchRequest),
        Required = true,
        Description = "The search request payload"
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.Accepted,
        contentType: "application/json",
        bodyType: typeof(SearchJob),
        Summary = "The accepted search job"
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.BadRequest,
        contentType: "application/json",
        bodyType: typeof(Problem),
        Summary = "Invalid search request"
    )]
    [RequiredScopes("find-record.write")]
    [Function(nameof(Searches))]
    public async Task<HttpResponseData> Searches(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/searches")]
            HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext context
    )
    {
        var searchRequest = await JsonSerializer.DeserializeAsync<StartSearchRequest>(req.Body);

        if (!StartSearchRequestValidator.IsValid(searchRequest, out var errorMessage))
        {
            var problem = new Problem(
                Type: "about:blank",
                Title: "Invalid Search Request",
                Detail: errorMessage,
                Status: (int)HttpStatusCode.BadRequest,
                Instance: $"urn:trace:{context.InvocationId}"
            );

            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(problem);

            return response;
        }

        logger.LogInformation("Requesting Search with Id: {Suid}", searchRequest?.Suid);

        var acceptedResponse = req.CreateResponse(HttpStatusCode.Accepted);

        var jobId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(SearchOrchestrator),
            searchRequest!.Suid
        );

        var searchJob = new SearchJob
        {
            JobId = jobId,
            Suid = searchRequest.Suid,
            Status = SearchStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
        };

        await acceptedResponse.WriteAsJsonAsync(searchJob);

        return acceptedResponse;
    }
}
