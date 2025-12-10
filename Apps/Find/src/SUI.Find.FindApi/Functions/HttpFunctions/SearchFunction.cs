using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.Domain.ValueObjects;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;
using SUI.Find.FindApi.Validators;

namespace SUI.Find.FindApi.Functions.HttpFunctions;

public class SearchFunction(ILogger<SearchFunction> logger, ISearchService searchService)
{
    [OpenApiOperation(
        operationId: "searches",
        tags: ["Searches"],
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
        FunctionContext context,
        CancellationToken cancellationToken
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
                context.InvocationId,
                cancellationToken
            );
        }

        var searchRequest = await JsonSerializer.DeserializeAsync<StartSearchRequest>(
            req.Body,
            JsonSerializerOptions.Web,
            cancellationToken
        );

        if (!StartSearchRequestValidator.IsValid(searchRequest, out var errorMessage))
        {
            return await HttpResponseUtility.BadRequestResponse(
                req,
                context.InvocationId,
                errorMessage,
                cancellationToken: cancellationToken
            );
        }

        logger.LogInformation("Requesting Search with Id: {Suid}", searchRequest?.Suid);

        var personId = new EncryptedPersonId(searchRequest!.Suid);
        var searchJob = await searchService.StartSearchAsync(
            personId,
            authContext.ClientId,
            authContext.Scopes.ToArray(),
            client,
            context.InvocationId,
            cancellationToken
        );

        return searchJob switch
        {
            SearchJobResult.Success s => await CreateSuccessResponse(req, s.Job, cancellationToken),
            SearchJobResult.Unauthorized => await HttpResponseUtility.UnauthorizedResponse(
                req,
                context.InvocationId,
                cancellationToken
            ),
            _ => await HttpResponseUtility.InternalServerErrorResponse(
                req,
                context.InvocationId,
                cancellationToken
            ),
        };
    }

    private static async Task<HttpResponseData> CreateSuccessResponse(
        HttpRequestData req,
        SearchJobDto result,
        CancellationToken cancellationToken
    )
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        var searchResults = SearchJob.FromDto(result);
        await response.WriteAsJsonAsync(searchResults, cancellationToken);
        return response;
    }
}
