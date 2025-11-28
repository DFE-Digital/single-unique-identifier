using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using Microsoft.OpenApi.Models;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.Functions.HttpTriggers;

public class SearchResultsFunction
{
    #region OpenApi
    [OpenApiOperation(
        operationId: "getSearchResults",
        tags: new[] { "Searches" },
        Summary = "Get search results",
        Description = "Returns the results for a Find a Record search job. Results MAY be available before the job reaches `completed` status, depending on implementation."
    )]
    [OpenApiParameter(
        name: "jobId",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "Identifier of the search job.",
        Description = "Identifier of the search job."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(SearchResults),
        Summary = "Results for the specified search job."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NotFound,
        contentType: "application/json",
        bodyType: typeof(Problem), // Replace with your actual model
        Summary = "Search job not found."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.InternalServerError,
        contentType: "application/json",
        bodyType: typeof(Problem), // Replace with your actual model
        Summary = "Error"
    )]
    #endregion
    [RequiredScopes("find-record.read")]
    [Function(nameof(SearchResultsTrigger))]
    public async Task<HttpResponseData> SearchResultsTrigger(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/searches/{jobId}/results")]
            HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        string jobId,
        CancellationToken cancellationToken
    )
    {
        // TODO: DI  the SearchService one the PR for it is merged
        throw new NotImplementedException();
    }
}
