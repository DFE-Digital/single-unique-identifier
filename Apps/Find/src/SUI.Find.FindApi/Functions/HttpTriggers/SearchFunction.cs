using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Functions.Orchestrators;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;
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
        if (
            !context.Items.TryGetValue(ApplicationConstants.Auth.AuthContextKey, out var authObj)
            || authObj is not AuthContext authContext
        )
        {
            return await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                "Unauthorized",
                "",
                context.InvocationId
            );
        }

        var searchRequest = await JsonSerializer.DeserializeAsync<StartSearchRequest>(req.Body);

        if (!StartSearchRequestValidator.IsValid(searchRequest, out var errorMessage))
        {
            return await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.BadRequest,
                "Invalid Search Request",
                errorMessage ?? "",
                $"urn:trace:{context.InvocationId}"
            );
        }

        logger.LogInformation("Requesting Search with Id: {Suid}", searchRequest?.Suid);

        var instanceId = $"{searchRequest!.Suid}-{authContext.ClientId}";
        var hashedInstanceId = HashUtility.HashInput(instanceId);

        var existingInstance = await client.GetInstanceAsync(hashedInstanceId);
        var hasExistingInstance =
            existingInstance is
            {
                RuntimeStatus: OrchestrationRuntimeStatus.Running
                    or OrchestrationRuntimeStatus.Pending
            };

        if (hasExistingInstance)
        {
            var originalJobId = existingInstance!.InstanceId;
            var jobStatus =
                existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Running
                    ? SearchStatus.Running
                    : SearchStatus.Queued;

            logger.LogInformation(
                "Duplicate Search Request for existing JobId: {JobId} with Status: {Status}. Returning existing job.",
                originalJobId,
                jobStatus
            );

            var originalJob = new SearchJob
            {
                JobId = originalJobId,
                Suid = searchRequest.Suid,
                Status = jobStatus,
                CreatedAt = existingInstance.CreatedAt,
                LastUpdatedAt = existingInstance.LastUpdatedAt,
            };
            var duplicateSearchResponse = req.CreateResponse(HttpStatusCode.Accepted);

            await duplicateSearchResponse.WriteAsJsonAsync(originalJob);
            logger.LogInformation(
                "Returning original Search Request with JobId: {JobId}",
                originalJobId
            );
            return duplicateSearchResponse;
        }

        var acceptedResponse = req.CreateResponse(HttpStatusCode.Accepted);

        var policyData = new PolicyContext(authContext.ClientId, Scopes: authContext.Scopes);

        var metaData = new SearchJobMetadata(
            PersonId: "TODO: Populate PersonId",
            RequestedAtUtc: DateTime.UtcNow
        );

        var jobId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(SearchOrchestrator),
            new SearchOrchestratorInput(searchRequest.Suid, metaData, policyData),
            new StartOrchestrationOptions { InstanceId = hashedInstanceId }
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
        logger.LogInformation("Creating a new Search Request with JobId: {JobId}", searchJob.JobId);
        return acceptedResponse;
    }
}
