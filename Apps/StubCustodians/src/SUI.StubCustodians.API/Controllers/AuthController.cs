using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.API.Controllers;

[ExcludeFromCodeCoverage]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class AuthController(ILogger<AuthController> logger) : ControllerBase
{
    private static readonly Lazy<AuthStore> Store = new(LoadAuthStore);

    private readonly int _tokenLifetimeMinutes =
        Store.Value.DefaultTokenLifetimeMinutes > 0 ? Store.Value.DefaultTokenLifetimeMinutes : 60;

    /// <summary>
    /// Issue a sandbox bearer token using client credentials (JSON)
    /// </summary>
    [HttpPost("token", Name = "TokenJson")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AuthTokenResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
    public Results<Ok<AuthTokenResponse>, ProblemHttpResult> AuthTokenJson(
        [FromBody] AuthTokenRequest request
    )
    {
        return ProcessTokenRequest(request.ClientId, request.ClientSecret, request.Scopes);
    }

    /// <summary>
    /// Issue a sandbox bearer token using client credentials (Form)
    /// </summary>
    [HttpPost("token", Name = "TokenForm")]
    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [ProducesResponseType(typeof(AuthTokenResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
    public Results<Ok<AuthTokenResponse>, ProblemHttpResult> AuthTokenForm(
        [FromForm] AuthTokenFormRequest request
    )
    {
        var scopes = string.IsNullOrWhiteSpace(request.scope)
            ? []
            : request
                .scope.Replace(",", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return ProcessTokenRequest(request.client_id, request.client_secret, scopes.AsEnumerable());
    }

    private Results<Ok<AuthTokenResponse>, ProblemHttpResult> ProcessTokenRequest(
        string clientId,
        string clientSecret,
        IEnumerable<string>? scopes
    )
    {
        logger.LogDebug(
            "Getting auth token: {clientId}, {clientSecret}, {scopes}",
            clientId,
            clientSecret,
            scopes
        );

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            logger.LogInformation("Missing clientId or clientSecret");
            return TypedResults.Problem(
                "Missing client_id or client_secret.",
                null,
                400,
                "Invalid request"
            );
        }

        var store = Store.Value;

        var client = store.Clients!.FirstOrDefault(c =>
            !string.IsNullOrWhiteSpace(c.ClientId)
            && c.ClientId.Equals(clientId, StringComparison.Ordinal)
        );

        if (client is null || !client.Enabled)
        {
            logger.LogInformation("Invalid client credentials");
            return TypedResults.Problem("Invalid client credentials.", null, 401, "Unauthorised");
        }

        if (
            string.IsNullOrWhiteSpace(client.ClientSecret)
            || !client.ClientSecret.Equals(clientSecret, StringComparison.Ordinal)
        )
        {
            logger.LogInformation("Invalid client credentials");
            return TypedResults.Problem("Invalid client credentials.", null, 401, "Unauthorised");
        }

        var requestedScopes = NormaliseScopes(scopes);
        var allowedScopes = NormaliseScopes(client.AllowedScopes);

        IReadOnlyList<string> grantedScopes;

        if (requestedScopes.Length == 0)
        {
            grantedScopes = allowedScopes!;
        }
        else
        {
            var notAllowed = requestedScopes
                .Where(s => !allowedScopes.Contains(s, StringComparer.Ordinal))
                .ToArray();

            if (notAllowed.Length > 0)
            {
                logger.LogInformation(
                    $"Client is not permitted to request scope(s): {string.Join(", ", notAllowed)}."
                );
                return TypedResults.Problem(
                    $"Client is not permitted to request scope(s): {string.Join(", ", notAllowed)}.",
                    null,
                    401,
                    "Invalid scope"
                );
            }

            grantedScopes = requestedScopes!;
        }

        var token = CreateJwt(store, client.ClientId, grantedScopes);

        logger.LogDebug("Returning token: {token}", token);
        return TypedResults.Ok(
            new AuthTokenResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresIn = _tokenLifetimeMinutes * 60,
                Scope = string.Join(' ', grantedScopes),
            }
        );
    }

    private string CreateJwt(AuthStore store, string clientId, IReadOnlyList<string> scopes)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_tokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Iss, store.Issuer),
            new Claim(JwtRegisteredClaimNames.Aud, store.Audience),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
            new Claim(
                JwtRegisteredClaimNames.Nbf,
                now.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
            new Claim(
                JwtRegisteredClaimNames.Exp,
                expires.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("client_id", clientId),
            new Claim("scp", string.Join(' ', scopes)),
        };

        var keyBytes = Encoding.UTF8.GetBytes(store.SigningKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: store.Issuer,
            audience: store.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static string?[] NormaliseScopes(IEnumerable<string>? incoming)
    {
        if (incoming is null)
        {
            return [];
        }

        return incoming
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static AuthStore LoadAuthStore()
    {
        var baseDir = AppContext.BaseDirectory;
        var filePath = Path.Combine(baseDir, "Data", "auth-clients.json");

        if (!System.IO.File.Exists(filePath))
        {
            throw new InvalidOperationException($"Auth store file not found at: {filePath}");
        }

        var json = System.IO.File.ReadAllText(filePath);
        var store = JsonSerializer.Deserialize<AuthStore>(json, JsonSerializerOptions.Web);

        if (store is null)
        {
            throw new InvalidOperationException("Auth store file could not be deserialised.");
        }

        if (
            string.IsNullOrWhiteSpace(store.Issuer)
            || string.IsNullOrWhiteSpace(store.Audience)
            || string.IsNullOrWhiteSpace(store.SigningKey)
        )
        {
            throw new InvalidOperationException(
                "Auth store file is missing issuer, audience, or signingKey."
            );
        }

        store.Clients ??= [];

        return store;
    }
}
