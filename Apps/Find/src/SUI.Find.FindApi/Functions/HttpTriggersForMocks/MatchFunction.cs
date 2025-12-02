using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;
using SUI.Find.FindApi.Validators;

namespace SUI.Find.FindApi.Functions.HttpTriggersForMocks;

[ExcludeFromCodeCoverage(Justification = "All mock implementation.")]
public class MatchFunction(ILogger<MatchFunction> logger)
{
    [Function(nameof(MatchPerson))]
    [RequiredScopes("match-record.read")]
    [OpenApiOperation(
        operationId: "FindPerson",
        tags: ["Match"],
        Summary = "Locate a persons unique id"
    )]
    [OpenApiRequestBody("application/json", typeof(MatchPersonRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(MatchPersonResponse))]
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

        MatchPersonRequest? model;
        try
        {
            model = await JsonSerializer.DeserializeAsync<MatchPersonRequest>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken
            );
        }
        catch
        {
            model = null;
        }

        if (model is null)
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

        var isValid = DataAnnotationValidator.Validate(model, out var validationResults);
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

        // TODO: Create service to get mock data.
        // TODO: Go get mock data and return if it matches the request.
        var response = req.CreateResponse(HttpStatusCode.OK);
        return response;
    }
}
