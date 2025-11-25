using Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Models;
using System.Net;
using System.Text.Json;

namespace Functions;

public sealed class MatchPersonFunction
{
    private readonly IFindOrchestrationService _orchestration;

    public MatchPersonFunction(IFindOrchestrationService orchestration)
    {
        _orchestration = orchestration;
    }

    [Function("MatchPerson")]
    [RequiredScopes("match-record.read")]
    [OpenApiOperation(
        operationId: "FindPerson",
        tags: new[] { "Match" },
        Summary = "Locate a persons unique id")]
    [OpenApiRequestBody("application/json", typeof(FindPersonRequest), Required = true)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(PersonMatch))]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/findperson")]
        HttpRequestData req,
        FunctionContext context)
    {
        if (!context.Items.TryGetValue("AuthContext", out var authObj) || authObj is not AuthContext auth)
        {
            var unauth = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauth.WriteAsJsonAsync(
                new Problem("about:blank", "Unauthorised", 401, "Missing or invalid bearer token.", null),
                context.CancellationToken);
            return unauth;
        }

        FindPersonRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<FindPersonRequest>(cancellationToken: context.CancellationToken);
        }
        catch (JsonException)
        {
            body = null;
        }

        if (body is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(
                new Problem("about:blank", "Invalid request", 400, "Request body is missing or malformed.", null),
                context.CancellationToken);
            return bad;
        }

        try
        {
            var match = await _orchestration.MatchPersonAsync(auth, body, context.CancellationToken);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(match, context.CancellationToken);
            return ok;
        }
        catch (ArgumentException ex)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(
                new Problem("about:blank", "Invalid request", 400, ex.Message, null),
                context.CancellationToken);
            return bad;
        }
        catch (KeyNotFoundException ex)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(
                new Problem("about:blank", "No exact match", 404, ex.Message, null),
                context.CancellationToken);
            return notFound;
        }
        catch (UnauthorizedAccessException ex)
        {
            var unauth = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauth.WriteAsJsonAsync(
                new Problem("about:blank", "Unauthorised", 401, ex.Message, null),
                context.CancellationToken);
            return unauth;
        }
    }
}
