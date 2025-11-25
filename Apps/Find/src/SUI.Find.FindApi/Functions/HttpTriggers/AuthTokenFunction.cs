using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Models.Auth;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.Functions.HttpTriggers;

public class AuthTokenFunction(
    ILogger<AuthTokenFunction> logger,
    IAuthStoreService authStoreService,
    IJwtTokenService jwtTokenService
)
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
            return await ProblemResponse(
                req,
                HttpStatusCode.BadRequest,
                "Invalid request",
                "Missing or malformed authentication details."
            );
        }

        var client = await ValidateAuthClientCredentialsAsync(req, authValidation.authValue);
        if (!client.IsValid)
        {
            return await ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                "Invalid client credentials."
            );
        }

        var scopes = await GetRequestScopesAsync(req);
        var requestedScopes = NormaliseScopes(scopes);
        var allowedScopes = NormaliseScopes(client.TokenRequest!.Scopes);

        IReadOnlyList<string> grantedScopes;
        if (requestedScopes.Count == 0)
        {
            grantedScopes = allowedScopes;
        }
        else
        {
            var notAllowed = requestedScopes
                .Where(s => !allowedScopes.Contains(s, StringComparer.Ordinal))
                .ToArray();

            if (notAllowed.Length > 0)
            {
                return await ProblemResponse(
                    req,
                    HttpStatusCode.BadRequest,
                    "Invalid scope",
                    $"Client is not permitted to request scope(s): {string.Join(", ", notAllowed)}."
                );
            }

            grantedScopes = requestedScopes;
        }

        var token = await jwtTokenService.GenerateToken(
            client.TokenRequest!.ClientId,
            grantedScopes
        );

        var tokenResponse = new AuthTokenResponse
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = 3600,
            Scope = string.Join(' ', grantedScopes),
        };
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(tokenResponse);
        return response;
    }

    private static async Task<string[]> GetRequestScopesAsync(HttpRequestData requestData)
    {
        var formData = await requestData.ReadAsStringAsync();
        var parsed = QueryHelpers.ParseQuery(formData);
        requestData.Body.Seek(0, SeekOrigin.Begin);
        var scopes = parsed.TryGetValue("scope", out var scopeValues)
            ? scopeValues.FirstOrDefault()?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? []
            : [];
        return scopes;
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
        var parsed = QueryHelpers.ParseQuery(formData);
        if (
            !parsed.TryGetValue("grant_type", out var grantTypes)
            || grantTypes.FirstOrDefault() != "client_credentials"
        )
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

    private async Task<(
        bool IsValid,
        AuthTokenRequest? TokenRequest
    )> ValidateAuthClientCredentialsAsync(HttpRequestData req, string authValue)
    {
        var base64Credentials = authValue["Basic ".Length..].Trim();
        var credentialBytes = Convert.FromBase64String(base64Credentials);
        var credentials = System.Text.Encoding.UTF8.GetString(credentialBytes).Split(':');
        if (credentials.Length != 2)
        {
            return (false, null);
        }

        var clientId = credentials.First();
        var clientSecret = credentials.Last();

        var authClient = await authStoreService.GetClientByCredentials(clientId, clientSecret);

        return (
            authClient.Success,
            new AuthTokenRequest
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scopes = authClient.Success ? authClient.Value?.AllowedScopes ?? [] : [],
            }
        );
    }

    private static IReadOnlyList<string> NormaliseScopes(IEnumerable<string>? incoming)
    {
        if (incoming is null)
        {
            return [];
        }

        return incoming
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.Ordinal)
            .ToList()!;
    }

    private static async Task<HttpResponseData> ProblemResponse(
        HttpRequestData req,
        HttpStatusCode code,
        string title,
        string detail
    )
    {
        var res = req.CreateResponse(code);
        await res.WriteAsJsonAsync(new Problem("about:blank", title, (int)code, detail, null));
        return res;
    }
}
