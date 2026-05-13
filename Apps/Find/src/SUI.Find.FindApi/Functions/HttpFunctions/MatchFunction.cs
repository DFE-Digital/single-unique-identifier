using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models.Matching;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Configurations;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.OpenApi;
using SUI.Find.FindApi.Utility;
using SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;

namespace SUI.Find.FindApi.Functions.HttpFunctions;

public class MatchFunction(
    ILogger<MatchFunction> logger,
    IMatchPersonOrchestrationService matchOrchestrationService,
    IIdRegisterRepository idRegisterRepository,
    IOptions<MatchFunctionConfiguration> matchFunctionConfig
)
{
    [Function(nameof(MatchPerson))]
    [RequiredScopes("match-record.read")]
    // Updated Summary
    [OpenApiOperation(
        operationId: "FindPerson",
        tags: ["Match"],
        Summary = "I know of this person, what is their Single Unique Identifier"
    )]
    // Wired Request Body Example
    [OpenApiRequestBody(
        "application/json",
        typeof(MatchRequest),
        Required = true,
        Example = typeof(MatchRequestExample)
    )]
    // Response Descriptions (and the new 500 Error)
    [OpenApiResponseWithBody(
        HttpStatusCode.OK,
        "application/json",
        typeof(PersonMatch),
        Description = "The requested demographic information confidently matched an individual person"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.BadRequest,
        "application/json",
        typeof(Problem),
        Description = "Request was refused because it contained invalid data, or was missing required data"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.Unauthorized,
        "application/json",
        typeof(Problem),
        Description = "Request was refused because it lacks valid authentication credentials"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.NotFound,
        "application/json",
        typeof(Problem),
        Description = "The requested demographic information did not confidently match an individual person"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.InternalServerError,
        "application/json",
        typeof(Problem),
        Description = "The server encountered an unexpected condition that prevented it from fulfilling the request"
    )]
    public async Task<HttpResponseData> MatchPerson(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/matchperson")]
            HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken
    )
    {
        using var logScope = logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = context.InvocationId }
        );

        if (
            !context.Items.TryGetValue(ApplicationConstants.Auth.AuthContextKey, out var authObj)
            || authObj is not AuthContext authContext
            || !VerifyApiKey(req)
        )
        {
            return await HttpResponseUtility.UnauthorizedResponse(
                req,
                context.InvocationId,
                cancellationToken
            );
        }

        var requestIsValid = TryGetMatchResponseRequestModel(req, out var request);

        if (!requestIsValid)
        {
            return await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.BadRequest,
                "Invalid request",
                "The request body is missing or malformed.",
                context.InvocationId,
                cancellationToken
            );
        }

        if (request.PersonSpecification is null)
        {
            return await HttpResponseUtility.BadRequestResponse(
                req,
                context.InvocationId,
                "PersonSpecification is required.",
                "Validation error",
                cancellationToken
            );
        }

        if (
            request.Metadata != null
            && request.Metadata.Any(k => string.IsNullOrWhiteSpace(k.RecordType))
        )
        {
            return await HttpResponseUtility.BadRequestResponse(
                req,
                context.InvocationId,
                "RecordType is mandatory for all Metadata entries.",
                "Validation error",
                cancellationToken
            );
        }

        var personMatch = await matchOrchestrationService.FindPersonIdAsync(
            request.PersonSpecification,
            authContext.ClientId,
            cancellationToken
        );

        return await personMatch.Match(
            async id =>
            {
                if (request.Metadata is not null)
                {
                    foreach (var entry in request.Metadata)
                    {
                        await idRegisterRepository.UpsertAsync(
                            new IdRegisterEntry
                            {
                                Sui = id.Value,
                                CustodianId = authContext.ClientId,
                                RecordType = entry.RecordType,
                                SystemId = entry.SystemId,
                                CustodianSubjectId = entry.RecordId,
                                Provenance = Provenance.AlreadyHeldByCustodian,
                                LastIdDeliveredAtUtc = DateTimeOffset.UtcNow,
                            },
                            cancellationToken
                        );
                    }
                }

                return await HttpResponseUtility.OkResponse(
                    req,
                    new PersonMatch(id.Value),
                    cancellationToken
                );
            },
            async dataValidationResult =>
                await HttpResponseUtility.BadRequestResponse(
                    req,
                    context.InvocationId,
                    JsonSerializer.Serialize(dataValidationResult),
                    "Validation error",
                    cancellationToken
                ),
            async notFound =>
                await HttpResponseUtility.NotFoundResponse(
                    req,
                    context.InvocationId,
                    cancellationToken
                ),
            async error =>
                await HttpResponseUtility.InternalServerErrorResponse(
                    req,
                    context.InvocationId,
                    cancellationToken
                )
        );
    }

    private bool TryGetMatchResponseRequestModel(HttpRequestData req, out MatchRequest model)
    {
        model = new MatchRequest { PersonSpecification = new PersonSpecification() };

        try
        {
            var requestBody = req.ReadAsString();

            var request = JsonSerializer.Deserialize<MatchRequest>(
                requestBody!,
                JsonSerializerOptions.Web
            );

            if (request is null)
            {
                return false;
            }

            model = request;
            return true;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse Match request: {ExMessage}", ex.Message);
            return false;
        }
    }

    private bool VerifyApiKey(HttpRequestData req)
    {
        if (!req.Headers.Contains("x-api-key"))
        {
            logger.LogInformation("Missing x-api-key header");
            return false;
        }

        var apiKey = req.Headers.GetValues("x-api-key").FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogInformation("Empty x-api-key header");
            return false;
        }

        if (apiKey != matchFunctionConfig.Value.XApiKey)
        {
            logger.LogWarning("Invalid x-api-key header value");
            return false;
        }

        return true;
    }
}
