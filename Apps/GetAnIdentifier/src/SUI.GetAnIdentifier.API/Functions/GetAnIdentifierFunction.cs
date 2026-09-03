using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SUI.GetAnIdentifier.API.Attributes;
using SUI.GetAnIdentifier.API.Configuration;
using SUI.GetAnIdentifier.API.Models;
using SUI.GetAnIdentifier.API.OpenApi;
using SUI.GetAnIdentifier.API.Utility;
using SUI.GetAnIdentifier.Application.Constants;
using SUI.GetAnIdentifier.Application.Interfaces;
using SUI.GetAnIdentifier.Application.Models;

namespace SUI.GetAnIdentifier.API.Functions;

public class GetAnIdentifierFunction(
    ILogger<GetAnIdentifierFunction> logger,
    IGetAnIdentifierService getAnIdentifierService,
    IAuditLogService auditLogService,
    IOptions<GetAnIdentifierConfiguration> matchFunctionConfig,
    TimeProvider timeProvider
)
{
    [Function(nameof(GetAnIdentifier))]
    [RequiredScopes("get-an-identifier.read")]
    // Updated Summary
    [OpenApiOperation(
        operationId: "GetAnIdentifier",
        Summary = "I know of this person, what is their Single Unique Identifier"
    )]
    [OpenApiSecurity(
        "function_key",
        SecuritySchemeType.ApiKey,
        Name = "code",
        In = OpenApiSecurityLocationType.Query
    )]
    // Wired Request Body Example
    [OpenApiRequestBody(
        "application/json",
        typeof(GetAnIdentifierRequest),
        Required = true,
        Example = typeof(GetAnIdentifierRequestExample)
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
        HttpStatusCode.BadGateway,
        "application/json",
        typeof(Problem),
        Description = "The upstream PDS matching service encountered an error or timed out"
    )]
    [OpenApiResponseWithBody(
        HttpStatusCode.InternalServerError,
        "application/json",
        typeof(Problem),
        Description = "The server encountered an unexpected condition that prevented it from fulfilling the request"
    )]
    public async Task<HttpResponseData> GetAnIdentifier(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/get-an-identifier")]
            HttpRequestData req,
        FunctionContext context,
        CancellationToken cancellationToken
    )
    {
        var correlationId = context.InvocationId.IsNullOrWhiteSpace()
            ? Guid.NewGuid().ToString()
            : context.InvocationId;

        using var logScope = logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = correlationId }
        );

        var clientId = TryGetClientId(context, out var isValidAuthContext);

        var requestIsValid = TryParseRequest(req, out var requestModel);

        // Audit incoming request
        await auditLogService.LogIncomingRequestAsync(
            clientId,
            correlationId,
            timeProvider.GetUtcNow(),
            req.Method,
            req.Url.AbsolutePath,
            requestIsValid ? requestModel : null,
            cancellationToken
        );

        if (!isValidAuthContext || !VerifyApiKey(req))
        {
            await auditLogService.LogOutgoingResponseAsync(
                clientId,
                correlationId,
                timeProvider.GetUtcNow(),
                (int)HttpStatusCode.Unauthorized,
                "Unauthorized",
                cancellationToken
            );
            return await HttpResponseUtility.UnauthorizedResponse(
                req,
                correlationId,
                cancellationToken
            );
        }

        if (!requestIsValid)
        {
            await auditLogService.LogOutgoingResponseAsync(
                clientId,
                correlationId,
                timeProvider.GetUtcNow(),
                (int)HttpStatusCode.BadRequest,
                "Invalid request - body missing or malformed",
                cancellationToken
            );
            return await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.BadRequest,
                "Invalid request",
                "The request body is missing or malformed.",
                correlationId,
                cancellationToken
            );
        }

        if (requestModel.PersonSpecification.IsNullOrDefault())
        {
            await auditLogService.LogOutgoingResponseAsync(
                clientId,
                correlationId,
                timeProvider.GetUtcNow(),
                (int)HttpStatusCode.BadRequest,
                "PersonSpecification is required",
                cancellationToken
            );
            return await HttpResponseUtility.BadRequestResponse(
                req,
                correlationId,
                "PersonSpecification is required.",
                "Validation error",
                cancellationToken
            );
        }

        if (
            requestModel.Metadata != null
            && requestModel.Metadata.Any(k => string.IsNullOrWhiteSpace(k.RecordType))
        )
        {
            await auditLogService.LogOutgoingResponseAsync(
                clientId,
                correlationId,
                timeProvider.GetUtcNow(),
                (int)HttpStatusCode.BadRequest,
                "RecordType is mandatory for all Metadata entries",
                cancellationToken
            );
            return await HttpResponseUtility.BadRequestResponse(
                req,
                correlationId,
                "RecordType is mandatory for all Metadata entries.",
                "Validation error",
                cancellationToken
            );
        }

        try
        {
            var personMatch = await getAnIdentifierService.MatchPersonAsync(
                requestModel.PersonSpecification,
                correlationId,
                cancellationToken
            );

            return await personMatch.Match(
                async getAnIdentifierResult =>
                {
                    await auditLogService.LogOutgoingResponseAsync(
                        clientId,
                        correlationId,
                        timeProvider.GetUtcNow(),
                        (int)HttpStatusCode.OK,
                        "Person matched successfully",
                        cancellationToken
                    );
                    return await HttpResponseUtility.OkResponse(
                        req,
                        PersonMatch.Create(getAnIdentifierResult),
                        cancellationToken
                    );
                },
                async dataValidationResult =>
                {
                    await auditLogService.LogOutgoingResponseAsync(
                        clientId,
                        correlationId,
                        timeProvider.GetUtcNow(),
                        (int)HttpStatusCode.BadRequest,
                        "Validation error",
                        cancellationToken
                    );
                    return await HttpResponseUtility.BadRequestResponse(
                        req,
                        correlationId,
                        JsonSerializer.Serialize(dataValidationResult),
                        "Validation error",
                        cancellationToken
                    );
                },
                async notFound =>
                {
                    await auditLogService.LogOutgoingResponseAsync(
                        clientId,
                        correlationId,
                        timeProvider.GetUtcNow(),
                        (int)HttpStatusCode.NotFound,
                        "NotFound",
                        cancellationToken
                    );
                    return await HttpResponseUtility.NotFoundResponse(
                        req,
                        correlationId,
                        cancellationToken
                    );
                },
                async error =>
                {
                    await auditLogService.LogOutgoingResponseAsync(
                        clientId,
                        correlationId,
                        timeProvider.GetUtcNow(),
                        (int)HttpStatusCode.BadGateway,
                        "Upstream PDS Error / Bad Gateway",
                        cancellationToken
                    );
                    return await HttpResponseUtility.ProblemResponse(
                        req,
                        HttpStatusCode.BadGateway,
                        "Upstream API Error",
                        "The upstream PDS matching service encountered an error or timed out. Matching cannot be completed at this time.",
                        correlationId,
                        cancellationToken
                    );
                }
            );
        }
        catch (Exception ex)
        {
            await auditLogService.LogOutgoingResponseAsync(
                clientId,
                correlationId,
                timeProvider.GetUtcNow(),
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                cancellationToken
            );
            logger.LogError(ex, "Unhandled exception during GetAnIdentifier execution");
            return await HttpResponseUtility.InternalServerErrorResponse(
                req,
                correlationId,
                cancellationToken
            );
        }
    }

    private static string TryGetClientId(FunctionContext context, out bool isValidAuthContext)
    {
        if (
            !context.Items.TryGetValue(ApplicationConstants.Auth.AuthContextKey, out var authObj)
            || authObj is not AuthContext authCtx
        )
        {
            isValidAuthContext = false;
            return string.Empty;
        }

        isValidAuthContext = true;
        return authCtx.ClientId;
    }

    private bool TryParseRequest(HttpRequestData req, out GetAnIdentifierRequest model)
    {
        model = new GetAnIdentifierRequest { PersonSpecification = new PersonSpecification() };

        try
        {
            var requestBody = req.ReadAsString();

            var request = JsonSerializer.Deserialize<GetAnIdentifierRequest>(
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
