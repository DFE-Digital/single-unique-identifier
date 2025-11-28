using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask;
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
        var authContext = context.Items.TryGetValue("AuthContext", out var authObj) ? authObj as AuthContext : null;

        var clientId = authContext?.ClientId ?? "unknown_client";

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

        var instanceId = $"{searchRequest!.Suid}-{clientId}";

        var existingInstance = await client.GetInstanceAsync(instanceId);
        var hasExistingInstance = existingInstance != null &&
                                  (existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Running ||
                                   existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Pending);
        if (hasExistingInstance)
        {
            var originalJobId = existingInstance?.InstanceId;
            var jobStatus = existingInstance?.RuntimeStatus == OrchestrationRuntimeStatus.Running
                ? SearchStatus.Running
                : SearchStatus.Queued;

            logger.LogInformation(
                "Duplicate Search Request for existing JobId: {JobId} with Status: {Status}. Returning existing job.",
                originalJobId,
                jobStatus
            );

            var originalJob = new SearchJob
            {
                JobId = originalJobId!,
                Suid = searchRequest.Suid,
                Status = jobStatus,
                CreatedAt = existingInstance!.CreatedAt,
                LastUpdatedAt = existingInstance.LastUpdatedAt,
                Links = new Dictionary<string, HalLink>
                {
                    { "self", new HalLink($"/v1/searches/{originalJobId}", "GET") },
                    { "results", new HalLink($"/v1/searches/{originalJobId}/results", "GET") },
                    { "cancel", new HalLink($"/v1/searches/{originalJobId}", "DELETE") },
                },
            };
            var duplicateSearchResponse = req.CreateResponse(HttpStatusCode.Accepted);

            await duplicateSearchResponse.WriteAsJsonAsync(originalJob);
            logger.LogInformation(
                "Returning original Search Request with JobId: {JobId}", originalJobId
            );
            return duplicateSearchResponse;
        }


        var acceptedResponse = req.CreateResponse(HttpStatusCode.Accepted);
        Console.WriteLine("Scheduled new orchestration with instanceId: {0}", instanceId);
        var jobId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(SearchOrchestrator),
            searchRequest.Suid,
            new StartOrchestrationOptions { InstanceId = instanceId }
        );
        Console.WriteLine("Scheduled new orchestration with JobId: {0}", jobId);
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
        logger.LogInformation(
            "Creating a new Search Request with JobId: {JobId}", searchJob.JobId
        );
        return acceptedResponse;
    }

}
