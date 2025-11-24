using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SUI.Find.FindApi.Functions.Orchestrators;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Validators;

namespace SUI.Find.FindApi.Functions.HttpTriggers;

public class SearchFunction(ILogger<SearchFunction> logger)
{
    #region OpenApiDocumentation
    [OpenApiOperation(
        operationId: "startSearch",
        tags: new[] { "Searches" },
        Summary = "Start a Find a Record search",
        Description = "Initiates an asynchronous search for existing records associated with the supplied SUID."
    )]
    [OpenApiRequestBody(
        contentType: "application/json",
        bodyType: typeof(StartSearchRequest),
        Required = true,
        Description = "Request to start a Find a Record search."
    )]
    [OpenApiParameter(
        name: "traceparent",
        In = ParameterLocation.Header,
        Required = false,
        Type = typeof(string),
        Summary = "W3C trace context 'traceparent' header for distributed tracing."
    )]
    [OpenApiParameter(
        name: "tracestate",
        In = ParameterLocation.Header,
        Required = false,
        Type = typeof(string),
        Summary = "W3C trace context 'tracestate' header for vendor-specific trace data."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.Accepted,
        contentType: "application/json",
        bodyType: typeof(SearchJob),
        Summary = "Search accepted."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.BadRequest,
        contentType: "application/json",
        bodyType: typeof(Problem),
        Summary = "Invalid request (e.g. missing or malformed SUID)."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.InternalServerError,
        contentType: "application/json",
        bodyType: typeof(Problem),
        Summary = "Error"
    )]
    #endregion
    [Function(nameof(Searches))]
    public async Task<HttpResponseData> Searches(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/searches")]
            HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext context
    )
    {
        var correlationId = context.InvocationId;
        using (
            logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId })
        )
        {
            var searchRequest = await JsonSerializer.DeserializeAsync<StartSearchRequest>(
                req.Body,
                JsonSerializerOptions.Web
            );

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

            logger.LogInformation("Requesting Search");

            var acceptedResponse = req.CreateResponse(HttpStatusCode.Accepted);

            var jobId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(SearchOrchestrator),
                new SearchOrchestratorParameters(
                    Suid: searchRequest!.Suid,
                    CorrelationId: correlationId
                )
            );

            var searchJob = new SearchJob
            {
                JobId = jobId,
                Suid = searchRequest.Suid,
                Status = SearchStatus.Queued,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                Links = new Dictionary<string, HalLink>
                {
                    { "self", new HalLink($"/v1/searches/{jobId}", "GET") },
                    { "results", new HalLink($"/v1/searches/{jobId}/results", "GET") },
                    { "cancel", new HalLink($"/v1/searches/{jobId}", "DELETE") },
                },
            };

            await acceptedResponse.WriteAsJsonAsync(searchJob);

            return acceptedResponse;
        }
    }
}
