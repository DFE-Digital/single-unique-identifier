using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.IdentityModel.Tokens;
using SUI.Find.CustodianSimulation.Models;

namespace SUI.Find.CustodianSimulation.Functions;

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class AuthTokenFunction
{
    private static readonly Lazy<AuthStore> Store = new(LoadAuthStore);

    private readonly int _tokenLifetimeMinutes =
        Store.Value.DefaultTokenLifetimeMinutes > 0 ? Store.Value.DefaultTokenLifetimeMinutes : 60;

    [Function("AuthToken")]
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
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/auth/token")]
            HttpRequestData req,
        FunctionContext context
    )
    {
        var body = await ReadAuthRequest(req);

        if (body is null)
        {
            return await ProblemResponse(
                req,
                HttpStatusCode.BadRequest,
                "Invalid request",
                "Missing or malformed token request."
            );
        }

        if (
            string.IsNullOrWhiteSpace(body.ClientId) || string.IsNullOrWhiteSpace(body.ClientSecret)
        )
        {
            return await ProblemResponse(
                req,
                HttpStatusCode.BadRequest,
                "Invalid request",
                "Missing client_id or client_secret."
            );
        }

        var store = Store.Value;

        var client = store.Clients!.FirstOrDefault(c =>
            !string.IsNullOrWhiteSpace(c.ClientId)
            && c.ClientId.Equals(body.ClientId, StringComparison.Ordinal)
        );

        if (client is null || !client.Enabled)
        {
            return await ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                "Invalid client credentials."
            );
        }

        if (
            string.IsNullOrWhiteSpace(client.ClientSecret)
            || !client.ClientSecret.Equals(body.ClientSecret, StringComparison.Ordinal)
        )
        {
            return await ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                "Invalid client credentials."
            );
        }

        var requestedScopes = NormaliseScopes(body.Scopes);
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
                return await ProblemResponse(
                    req,
                    HttpStatusCode.BadRequest,
                    "Invalid scope",
                    $"Client is not permitted to request scope(s): {string.Join(", ", notAllowed)}."
                );
            }

            grantedScopes = requestedScopes!;
        }

        var token = CreateJwt(store, client.ClientId, grantedScopes);

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(
            new AuthTokenResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresIn = _tokenLifetimeMinutes * 60,
                Scope = string.Join(' ', grantedScopes),
            }
        );
        return res;
    }

    private static async Task<AuthTokenRequest?> ReadAuthRequest(HttpRequestData req)
    {
        var raw = await new StreamReader(req.Body, Encoding.UTF8).ReadToEndAsync();

        var contentType = req.Headers.TryGetValues("Content-Type", out var values)
            ? values.FirstOrDefault() ?? string.Empty
            : string.Empty;

        var looksLikeForm = raw.Contains('=');

        if (
            contentType.StartsWith(
                "application/x-www-form-urlencoded",
                StringComparison.OrdinalIgnoreCase
            ) || looksLikeForm
        )
        {
            var form = ParseForm(raw);

            form.TryGetValue("grant_type", out var grantType);
            if (
                !string.IsNullOrWhiteSpace(grantType)
                && !grantType.Equals("client_credentials", StringComparison.OrdinalIgnoreCase)
            )
            {
                return null;
            }

            form.TryGetValue("client_id", out var clientId);
            form.TryGetValue("client_secret", out var clientSecret);
            form.TryGetValue("scope", out var scopeRaw);

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                (clientId, clientSecret) = TryReadBasicClientCredentials(req);
            }

            var scopes = string.IsNullOrWhiteSpace(scopeRaw)
                ? []
                : scopeRaw
                    .Replace(",", " ")
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    );

            return new AuthTokenRequest
            {
                ClientId = clientId ?? string.Empty,
                ClientSecret = clientSecret ?? string.Empty,
                Scopes = scopes,
            };
        }

        try
        {
            return JsonSerializer.Deserialize<AuthTokenRequest>(raw, JsonSerializerOptions.Web);
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, string> ParseForm(string raw)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(raw))
        {
            return dict;
        }

        var pairs = raw.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in pairs)
        {
            var idx = p.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(p.Substring(0, idx));
            var valuePart = p.Substring(idx + 1).Replace("+", " ");
            var value = Uri.UnescapeDataString(valuePart);

            dict[key] = value;
        }

        return dict;
    }

    private static (string? ClientId, string? ClientSecret) TryReadBasicClientCredentials(
        HttpRequestData req
    )
    {
        if (!req.Headers.TryGetValues("Authorization", out var authValues))
        {
            return (null, null);
        }

        var authHeader = authValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            return (null, null);
        }

        if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        var encoded = authHeader.Substring("Basic ".Length).Trim();

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var sep = decoded.IndexOf(':');
            if (sep <= 0)
            {
                return (null, null);
            }

            var clientId = decoded.Substring(0, sep);
            var clientSecret = decoded.Substring(sep + 1);

            return (clientId, clientSecret);
        }
        catch
        {
            return (null, null);
        }
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

        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"Auth store file not found at: {filePath}");
        }

        var json = File.ReadAllText(filePath);
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
