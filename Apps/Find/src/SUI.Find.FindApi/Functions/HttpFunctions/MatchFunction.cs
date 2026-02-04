using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models.Matching;
using SUI.Find.Application.Services;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Configurations;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;

namespace SUI.Find.FindApi.Functions.HttpFunctions;

public class MatchFunction(
    ILogger<MatchFunction> logger,
    IMatchPersonOrchestrationService matchOrchestrationService,
    IOptions<MatchFunctionConfiguration> matchFunctionConfig
)
{
    [Function(nameof(MatchPerson))]
    [RequiredScopes("match-record.read")]
    [OpenApiOperation(
        operationId: "FindPerson",
        tags: ["Match"],
        Summary = "I know of this person, what is their unique ID"
    )]
    [OpenApiRequestBody("application/json", typeof(PersonSpecification), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(PersonMatch))]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
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

        var personMatch = await matchOrchestrationService.FindPersonIdAsync(
            request,
            authContext.ClientId,
            cancellationToken
        );
        return await personMatch.Match(
            id => CreateOkResponse(req, id),
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

    private static bool TryGetMatchResponseRequestModel(
        HttpRequestData req,
        out PersonSpecification model
    )
    {
        model = new PersonSpecification();
        try
        {
            var requestBody = req.ReadAsString();

            var request = JsonSerializer.Deserialize<PersonSpecification>(
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
        catch (JsonException)
        {
            return false;
        }
    }

    private static async Task<HttpResponseData> CreateOkResponse(
        HttpRequestData req,
        PersonIdValue encryptedPersonId
    )
    {
        var res = req.CreateResponse(HttpStatusCode.OK);
        var responseBody = new PersonMatch(encryptedPersonId.Value);
        await res.WriteAsJsonAsync(responseBody);
        return res;
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
