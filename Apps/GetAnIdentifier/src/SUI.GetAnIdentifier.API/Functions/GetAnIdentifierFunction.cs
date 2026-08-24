using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
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
    IOptions<GetAnIdentifierConfiguration> matchFunctionConfig
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
        var correlationId = GetCorrelationId(req, context);

        using var logScope = logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = correlationId }
        );

        string callerId = "unauthenticated";
        if (
            context.Items.TryGetValue(ApplicationConstants.Auth.AuthContextKey, out var authObj)
            && authObj is AuthContext authCtx
        )
        {
            callerId = authCtx.ClientId;
        }

        var requestIsValid = TryGetMatchResponseRequestModel(req, out var request);

        // Audit incoming request
        var incomingTimestamp = DateTimeOffset.UtcNow;
        await auditLogService.LogIncomingRequestAsync(
            callerId,
            correlationId,
            incomingTimestamp,
            req.Method,
            GetSafeAbsolutePath(req),
            requestIsValid ? request : null,
            cancellationToken
        );

        HttpResponseData response;
        string responseSummary;

        if (
            !context.Items.TryGetValue(ApplicationConstants.Auth.AuthContextKey, out _)
            || authObj is not AuthContext
            || !VerifyApiKey(req)
        )
        {
            response = await HttpResponseUtility.UnauthorizedResponse(
                req,
                correlationId,
                cancellationToken
            );
            responseSummary = "Unauthorized";
            await LogResponseAndReturnAsync(
                callerId,
                correlationId,
                response.StatusCode,
                responseSummary,
                cancellationToken
            );
            return response;
        }

        if (!requestIsValid)
        {
            response = await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.BadRequest,
                "Invalid request",
                "The request body is missing or malformed.",
                correlationId,
                cancellationToken
            );
            responseSummary = "Invalid request - body missing or malformed";
            await LogResponseAndReturnAsync(
                callerId,
                correlationId,
                response.StatusCode,
                responseSummary,
                cancellationToken
            );
            return response;
        }

        if (request.PersonSpecification is null)
        {
            response = await HttpResponseUtility.BadRequestResponse(
                req,
                correlationId,
                "PersonSpecification is required.",
                "Validation error",
                cancellationToken
            );
            responseSummary = "PersonSpecification is required";
            await LogResponseAndReturnAsync(
                callerId,
                correlationId,
                response.StatusCode,
                responseSummary,
                cancellationToken
            );
            return response;
        }

        if (
            request.Metadata != null
            && request.Metadata.Any(k => string.IsNullOrWhiteSpace(k.RecordType))
        )
        {
            response = await HttpResponseUtility.BadRequestResponse(
                req,
                correlationId,
                "RecordType is mandatory for all Metadata entries.",
                "Validation error",
                cancellationToken
            );
            responseSummary = "RecordType is mandatory for all Metadata entries";
            await LogResponseAndReturnAsync(
                callerId,
                correlationId,
                response.StatusCode,
                responseSummary,
                cancellationToken
            );
            return response;
        }

        try
        {
            var personMatch = await getAnIdentifierService.MatchPersonAsync(
                request.PersonSpecification,
                correlationId,
                cancellationToken
            );

            responseSummary = string.Empty;
            response = await personMatch.Match(
                async getAnIdentifierResult =>
                {
                    responseSummary = "Person matched successfully";
                    return await HttpResponseUtility.OkResponse(
                        req,
                        PersonMatch.Create(getAnIdentifierResult),
                        cancellationToken
                    );
                },
                async dataValidationResult =>
                {
                    responseSummary = "Validation error";
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
                    responseSummary = "Not Found";
                    return await HttpResponseUtility.NotFoundResponse(
                        req,
                        correlationId,
                        cancellationToken
                    );
                },
                async error =>
                {
                    responseSummary = "Upstream PDS Error / Bad Gateway";
                    return await HttpResponseUtility.ProblemResponse(
                        req,
                        HttpStatusCode.BadGateway,
                        "Downstream API Error",
                        "The upstream PDS matching service encountered an error or timed out. Matching cannot be completed at this time.",
                        correlationId,
                        cancellationToken
                    );
                }
            );

            await LogResponseAndReturnAsync(
                callerId,
                correlationId,
                response.StatusCode,
                responseSummary,
                cancellationToken
            );
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception during GetAnIdentifier execution");
            response = await HttpResponseUtility.InternalServerErrorResponse(
                req,
                correlationId,
                cancellationToken
            );
            responseSummary = "Internal Server Error";
            await LogResponseAndReturnAsync(
                callerId,
                correlationId,
                response.StatusCode,
                responseSummary,
                cancellationToken
            );
            return response;
        }
    }

    private Task LogResponseAndReturnAsync(
        string callerId,
        string correlationId,
        HttpStatusCode statusCode,
        string summary,
        CancellationToken cancellationToken
    )
    {
        return auditLogService.LogOutgoingResponseAsync(
            callerId,
            correlationId,
            DateTimeOffset.UtcNow,
            (int)statusCode,
            summary,
            cancellationToken
        );
    }

    private static string GetCorrelationId(HttpRequestData req, FunctionContext context)
    {
        if (req.Headers.TryGetValues("X-Correlation-ID", out var corValues))
        {
            var first = corValues.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            if (!string.IsNullOrWhiteSpace(first))
                return first;
        }

        if (req.Headers.TryGetValues("X-Request-ID", out var reqValues))
        {
            var first = reqValues.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            if (!string.IsNullOrWhiteSpace(first))
                return first;
        }

        return context.InvocationId;
    }

    private bool TryGetMatchResponseRequestModel(
        HttpRequestData req,
        out GetAnIdentifierRequest model
    )
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

    private static string GetSafeAbsolutePath(HttpRequestData req)
    {
        try
        {
            return req.Url?.AbsolutePath ?? string.Empty;
        }
        catch (UriFormatException)
        {
            return string.Empty;
        }
    }
}
