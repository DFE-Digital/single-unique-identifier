using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.Find.Application.Configurations;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Models;
using SUI.Find.Domain.ValueObjects;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;
using SUI.Find.FindApi.Validators;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.FindApi.Functions.HttpFunctions;

public class SearchFunctionV2(
    ILogger<SearchFunctionV2> logger,
    IJobQueueService findQueueService,
    IOptions<EncryptionConfiguration> encryptionConfig
)
{
    [OpenApiOperation(
        operationId: "searches-v2",
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
    [Function(nameof(SearchesV2))]
    public async Task<HttpResponseData> SearchesV2(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v2/searches")]
            HttpRequestData req,
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
        var workItemId = Guid.NewGuid();
        using var scope = logger.BeginScope(
            new Dictionary<string, object>
            {
                { "WorkItemId", workItemId },
                { "PersonId", searchRequest?.Suid ?? string.Empty },
                { "RequestingCustodianId", authContext.ClientId },
                { "TraceParent", context.TraceContext.TraceParent },
                { "TraceId", context.TraceContext.TraceState },
                { "InvocationId", context.InvocationId },
            }
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

        var payload = new SearchRequestMessage
        {
            WorkItemId = workItemId,
            PersonId = personId,
            RequestingCustodianId = authContext.ClientId,
            TraceParent = context.TraceContext.TraceParent,
            TraceId = Activity.Current?.TraceId.ToString() ?? string.Empty,
            InvocationId = context.InvocationId,
        };

        var result = await findQueueService.PostSearchJobAsync(payload, cancellationToken);

        return await CreateSuccessResponse(req, result, cancellationToken);
    }

    private static async Task<HttpResponseData> CreateSuccessResponse(
        HttpRequestData req,
        SearchJobDto result,
        CancellationToken cancellationToken
    )
    {
        var response = req.CreateResponse(HttpStatusCode.Accepted);
        response.Headers.Add("Cache-Control", "no-store, no-cache, max-age=0, must-revalidate");
        response.Headers.Add("Pragma", "no-cache");
        response.Headers.Add("Expires", DateTime.MinValue.ToUniversalTime().ToString("R"));
        response.Headers.Add("Vary", "Authorization");
        var searchResults = SearchJob.FromDto(result);
        await response.WriteAsJsonAsync(searchResults, cancellationToken);
        return response;
    }
}
