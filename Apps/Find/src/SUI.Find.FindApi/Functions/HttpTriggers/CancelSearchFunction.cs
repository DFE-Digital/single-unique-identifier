using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;

namespace SUI.Find.FindApi.Functions.HttpTriggers;

public class CancelSearchFunction(ILogger<SearchFunction> logger)
{
    [OpenApiOperation(
        operationId: "cancelSearch",
        tags: ["Searches"],
        Summary = "Cancel a search job"
    )]
    [OpenApiParameter(
        name: "jobId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "Job ID",
        Description = "The unique identifier for the search job."
    )]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.NoContent,
        Summary = "Search job cancelled"
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NotFound,
        contentType: "application/json",
        bodyType: typeof(Problem),
        Summary = "Search job not found"
    )]
    [RequiredScopes("find-record.write")]
    [Function(nameof(CancelSearch))]
    public async Task<HttpResponseData> CancelSearch(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "v1/searches/{jobId}")]
            HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        string jobId,
        FunctionContext context,
        CancellationToken cancellationToken
    )
    {
        var metaData = await client.GetInstanceAsync(jobId, cancellation: cancellationToken);
        if (metaData is null)
        {
            var problem = new Problem(
                Type: "about:blank",
                Title: "Not Found",
                Detail: $"No search job found with id '{jobId}'.",
                Status: (int)HttpStatusCode.NotFound,
                Instance: $"urn:trace:{context.InvocationId}"
            );
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(problem, cancellationToken);
            return notFound;
        }

        if (metaData.IsCompleted)
        {
            var details = $"Cannot cancel search job with id '{jobId}' as it is already completed.";
            return await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.Conflict,
                "Conflict",
                details,
                $"urn:trace:{context.InvocationId}",
                cancellationToken
            );
        }

        try
        {
            await client.TerminateInstanceAsync(jobId, cancellation: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling search job with id: {JobId}", jobId);
        }

        logger.LogInformation("Cancelled search job with id: {JobId}", jobId);

        return req.CreateResponse(HttpStatusCode.NoContent);
    }
}
