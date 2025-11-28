using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SUI.Find.Application.Enums;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;

namespace SUI.Find.FindApi.Functions.HttpTriggers;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class SearchStatusFunction(ILogger<SearchStatusFunction> logger)
{
    #region OpenAPI
    [OpenApiOperation(
        operationId: "getSearch",
        tags: ["Searches"],
        Summary = "Get search status",
        Description = "Retrieve the current status of a Find a Record search job."
    )]
    [OpenApiParameter(
        name: "jobId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "Identifier of the search job.",
        Description = "The unique identifier for the search job."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(SearchJob),
        Summary = "Current search job status.",
        Description = "Returns the search job details if found."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NotFound,
        contentType: "application/json",
        bodyType: typeof(Problem),
        Summary = "Search job not found.",
        Description = "Returned when the search job does not exist."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.InternalServerError,
        contentType: "application/json",
        bodyType: typeof(Problem),
        Summary = "Error",
        Description = "Error response."
    )]
    #endregion
    [RequiredScopes("find-record.read")]
    [Function(nameof(SearchJobTrigger))]
    public async Task<HttpResponseData> SearchJobTrigger(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/searches/{jobId}")]
            HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        string jobId,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Requesting Search Job Status for Id: {JobId}", jobId);
        var jobStatus = await client.GetInstanceAsync(jobId, true, cancellationToken);
        if (jobStatus is null)
        {
            return await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.NotFound,
                "Not Found",
                $"No search job found with Id: {jobId}",
                context.InvocationId,
                cancellationToken
            );
        }

        var response = req.CreateResponse(HttpStatusCode.OK);

        var searchJob = CreateSearchJobFromMetadata(jobStatus);

        await response.WriteAsJsonAsync(searchJob, cancellationToken);
        return response;
    }

    private SearchJob CreateSearchJobFromMetadata(OrchestrationMetadata jobStatus)
    {
        return new SearchJob
        {
            JobId = jobStatus.InstanceId,
            Suid = GetSuidFromJobStatus(jobStatus),
            Status = ConvertOrchestrationStatusToSearchStatus(jobStatus),
            CreatedAt = jobStatus.CreatedAt,
            LastUpdatedAt = jobStatus.LastUpdatedAt,
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Sealed method that is hard to mock in unit tests")]
    // Made virtual to allow mocking in unit tests
    public virtual string GetSuidFromJobStatus(OrchestrationMetadata jobStatus)
    {
        return jobStatus.ReadInputAs<string>() ?? string.Empty;
    }

    private static SearchStatus ConvertOrchestrationStatusToSearchStatus(
        OrchestrationMetadata status
    )
    {
        return status.RuntimeStatus switch
        {
            OrchestrationRuntimeStatus.Pending => SearchStatus.Queued,
            OrchestrationRuntimeStatus.Running => SearchStatus.Running,
            OrchestrationRuntimeStatus.Completed => SearchStatus.Completed,
            OrchestrationRuntimeStatus.Terminated => SearchStatus.Cancelled,
            _ => SearchStatus.Failed,
        };
    }
}
