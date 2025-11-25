using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Models.Auth;

namespace SUI.Find.FindApi.Functions.HttpTriggers;

public class AuthTokenFunction(ILogger<AuthTokenFunction> logger)
{
    [Function(nameof(AuthToken))]
    [OpenApiOperation(
        operationId: "authToken",
        tags: ["Auth"],
        Summary = "Issue a sandbox bearer token using client credentials"
    )]
    [OpenApiRequestBody("application/json", typeof(AuthTokenRequest), Required = true)]
    [OpenApiRequestBody(
        "application/x-www-form-urlencoded",
        typeof(AuthTokenFormRequest),
        Required = false
    )]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(AuthTokenResponse))]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(Problem))]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, "application/json", typeof(Problem))]
    public async Task<HttpResponseData> AuthToken(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/auth/token")]
            HttpRequestData req,
        FunctionContext context
    )
    {
        var authValidation = await ValidateAuthRequestAsync(req);
        if (!authValidation.isValid)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(
                new Problem(
                    "about:blank",
                    "Invalid request",
                    400,
                    "Missing or malformed authentication details.",
                    null
                )
            );
            return badResponse;
        }

        var isValidClient = ValidateAuthClientCredentials(authValidation.authValue);
        if (!isValidClient)
        {
            var unauthenticResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthenticResponse.WriteAsJsonAsync(
                new Problem("about:blank", "Unauthorised", 401, "Invalid client credentials.", null)
            );
            return unauthenticResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        return response;
    }

    private static async Task<(bool isValid, string authValue)> ValidateAuthRequestAsync(
        HttpRequestData requestData
    )
    {
        var contentType = requestData.Headers.TryGetValues("Content-Type", out var values)
            ? values.FirstOrDefault() ?? string.Empty
            : string.Empty;

        if (!contentType.Contains("application/x-www-form-urlencoded"))
        {
            return (false, string.Empty);
        }

        var formData = await requestData.ReadAsStringAsync();
        if (formData is null || !formData.Contains("grant_type=client_credentials"))
        {
            return (false, string.Empty);
        }

        var hasAuthHeader = requestData.Headers.TryGetValues("Authorization", out var authValues);
        if (!hasAuthHeader || authValues is null)
        {
            return (false, string.Empty);
        }

        var authHeader = authValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Basic "))
        {
            return (false, string.Empty);
        }

        requestData.Body.Seek(0, SeekOrigin.Begin);

        return (true, authHeader);
    }

    private static bool ValidateAuthClientCredentials(string authValue)
    {
        // get value out of base64
        var base64Credentials = authValue["Basic ".Length..].Trim();
        var credentialBytes = Convert.FromBase64String(base64Credentials);
        var credentials = System.Text.Encoding.UTF8.GetString(credentialBytes).Split(':');
        if (credentials.Length != 2)
        {
            return false;
        }

        var clientId = credentials.First();
        var clientSecret = credentials.Last();

        var isValid = !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret);

        // TODO: Check  the clientId and clientSecret against a data store

        return isValid;
    }
}
