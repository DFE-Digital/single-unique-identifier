using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.Find.Application.Configurations;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.Domain.ValueObjects;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;
using SUI.Find.FindApi.Validators;

namespace SUI.Find.FindApi.Functions.HttpFunctions;

public class SearchFunction(
    ILogger<SearchFunction> logger,
    ISearchService searchService,
    IOptions<EncryptionConfiguration> encryptionConfig
)
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

        var encrypt = encryptionConfig.Value.EnablePersonIdEncryption;

        if (!StartSearchRequestValidator.IsValid(searchRequest, encrypt, out var errorMessage))
        {
            return await HttpResponseUtility.BadRequestResponse(
                req,
                context.InvocationId,
                errorMessage,
                cancellationToken: cancellationToken
            );
        }

        logger.LogInformation("Requesting Search with Id: {Suid}", searchRequest?.Suid);
        var personId = string.Empty;

        if (encrypt)
        {
            var encryptedPersonIdResult = EncryptedPersonId.Create(searchRequest!.Suid);
            personId = encryptedPersonIdResult.Value!.Value;
        }
        else
        {
            if (searchRequest?.Suid != null)
                personId = searchRequest.Suid;
        }

        var searchJob = await searchService.StartSearchAsync(
            personId,
            authContext.ClientId,
            client,
            context.InvocationId,
            cancellationToken
        );

        return await searchJob.Match<Task<HttpResponseData>>(
            async dto =>
                await HttpResponseUtility.AcceptedResponse(
                    req,
                    SearchJob.FromDto(dto),
                    cancellationToken
                ),
            async _ =>
                await HttpResponseUtility.InternalServerErrorResponse(
                    req,
                    context.InvocationId,
                    cancellationToken
                )
        );
    }
}
