using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SUI.AuthEmulator.Models;
using SUI.AuthEmulator.Services;

namespace SUI.AuthEmulator.Controllers;

[ApiController]
[Route("api/v1/[controller]/")]
public class AuthController(
    ILogger<AuthController> logger,
    IAuthStoreService authStoreService,
    IJwtTokenService jwtTokenService
) : ControllerBase
{
    /// <summary>
    /// Issue a sandbox bearer token using client credentials
    /// </summary>
    [HttpPost("token", Name = "Token")]
    [Consumes("application/x-www-form-urlencoded", "application/json")]
    [ProducesResponseType(typeof(AuthTokenResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
    public async Task<Results<Ok<AuthTokenResponse>, ProblemHttpResult>> AuthToken()
    {
        var authValidation = ValidateAuthRequest();
        if (!authValidation.isValid)
        {
            return TypedResults.Problem(
                "Missing or malformed authentication details.",
                null,
                (int)HttpStatusCode.BadRequest,
                "Invalid request"
            );
        }

        var authClient = await ValidateAuthClientCredentialsAsync(authValidation.authValue);
        if (!authClient.IsValid)
        {
            return TypedResults.Problem(
                "Invalid client credentials.",
                null,
                (int)HttpStatusCode.Unauthorized,
                nameof(HttpStatusCode.Unauthorized)
            );
        }

        var queryScopes = GetRequestScopeFromFormAsync();
        var requestedScopes = NormaliseScopes(queryScopes);
        var allowedScopes = NormaliseScopes(authClient.TokenRequest!.Scopes);

        IReadOnlyList<string> grantedScopes;
        if (requestedScopes.Length == 0)
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
                return TypedResults.Problem(
                    $"Client is not permitted to request scope(s): {string.Join(", ", notAllowed)}.",
                    null,
                    (int)HttpStatusCode.BadRequest,
                    "Invalid scope"
                );
            }

            grantedScopes = requestedScopes;
        }

        var modeHeader = HttpContext.Request.Headers["mode"].FirstOrDefault();

        // Pass the mode string to the token service
        var token = jwtTokenService.GenerateToken(
            authClient.TokenRequest!.ClientId,
            grantedScopes,
            modeHeader
        );

        return TypedResults.Ok(
            new AuthTokenResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresIn = 3600,
                Scope = string.Join(' ', grantedScopes),
            }
        );
    }

    private string[] GetRequestScopeFromFormAsync()
    {
        if (HttpContext.Request.Form.TryGetValue("scope", out var scopeValues))
        {
            return scopeValues.FirstOrDefault()?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                ?? [];
        }
        return [];
    }

    private (bool isValid, string authValue) ValidateAuthRequest()
    {
        var contentType = HttpContext.Request.Headers.ContentType.FirstOrDefault() ?? string.Empty;

        if (
            !contentType.Contains("application/x-www-form-urlencoded")
            || !HttpContext.Request.Form.TryGetValue("grant_type", out var grantTypes)
            || grantTypes.FirstOrDefault() != "client_credentials"
        )
            return (false, string.Empty);

        var authValues = HttpContext.Request.Headers.Authorization;
        if (authValues.Count == 0)
        {
            logger.LogWarning("Missing Authorization header.");
            return (false, string.Empty);
        }

        var authHeader = authValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Basic "))
        {
            logger.LogWarning("Invalid Authorization header.");
            return (false, string.Empty);
        }

        return (true, authHeader);
    }

    private async Task<(
        bool IsValid,
        AuthTokenRequest? TokenRequest
    )> ValidateAuthClientCredentialsAsync(string authValue)
    {
        var base64Credentials = authValue["Basic ".Length..].Trim();
        var credentialBytes = Convert.FromBase64String(base64Credentials);
        var credentials = System.Text.Encoding.UTF8.GetString(credentialBytes).Split(':');
        if (credentials.Length != 2)
        {
            return (false, null);
        }

        var clientId = credentials[0];
        var clientSecret = credentials[^1];

        if (string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(clientId))
        {
            logger.LogWarning("ClientId or ClientSecret is missing.");
            return (false, null);
        }

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

    private static string[] NormaliseScopes(IEnumerable<string>? incoming)
    {
        if (incoming is null)
        {
            return [];
        }

        return incoming
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
