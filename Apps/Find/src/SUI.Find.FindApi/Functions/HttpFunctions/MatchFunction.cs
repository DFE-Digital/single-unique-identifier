using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
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

public class MatchFunction(ILogger<MatchFunction> logger, IMatchingService service)
{
    [Function(nameof(MatchPerson))]
    [RequiredScopes("match-record.read")]
    [OpenApiOperation(
        operationId: "FindPerson",
        tags: ["Match"],
        Summary = "Locate a persons unique id"
    )]
    [OpenApiRequestBody("application/json", typeof(MatchPersonRequest), Required = true)]
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

        var isValid = DataAnnotationValidator.Validate(request, out var validationResults);
        if (!isValid)
        {
            return await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.BadRequest,
                "Invalid request",
                validationResults ?? "The request model is invalid.",
                context.InvocationId,
                cancellationToken
            );
        }

        var personMatch = await service.MatchPersonAsync(request, authContext.ClientId);
        return await personMatch.Match(
            encryptedPersonId => CreateOkResponse(req, encryptedPersonId),
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
        out MatchPersonRequest model
    )
    {
        model = new MatchPersonRequest();
        try
        {
            var requestBody = req.ReadAsString();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return false;
            }

            var request = JsonSerializer.Deserialize<MatchPersonRequest>(
                requestBody,
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
        EncryptedPersonId encryptedPersonId
    )
    {
        var res = req.CreateResponse(HttpStatusCode.OK);
        var responseBody = new PersonMatch(encryptedPersonId.Value);
        await res.WriteAsJsonAsync(responseBody);
        return res;
    }
}
