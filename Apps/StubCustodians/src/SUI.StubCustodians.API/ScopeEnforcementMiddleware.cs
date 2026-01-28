using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.API;

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public class ScopeEnforcementMiddleware
{
    private static readonly JwtSecurityTokenHandler Handler = new();
    private static readonly Lazy<AuthStore> Store = new(LoadAuthStore);
    private readonly RequestDelegate _next;

    public ScopeEnforcementMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requiredScopes = GetRequiredScopes(context);

        if (requiredScopes.Length == 0)
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeaders))
        {
            await ProblemResponse(
                context,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                "Missing Authorization header."
            );
            return;
        }

        var header = authHeaders.FirstOrDefault();
        if (
            string.IsNullOrWhiteSpace(header)
            || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        )
        {
            await ProblemResponse(
                context,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                "Missing or invalid bearer token."
            );
            return;
        }

        var token = header["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            await ProblemResponse(
                context,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                "Missing bearer token."
            );
            return;
        }

        var store = Store.Value;

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = store.Issuer,
            ValidateAudience = true,
            ValidAudience = store.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(store.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        };

        try
        {
            Handler.ValidateToken(token, validationParameters, out _);
        }
        catch (SecurityTokenException ex)
        {
            await ProblemResponse(context, HttpStatusCode.Unauthorized, "Unauthorised", ex.Message);
            return;
        }
        catch
        {
            await ProblemResponse(
                context,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                "Invalid bearer token."
            );
            return;
        }

        var authContext = AuthContextFactory.FromAuthorizationHeader(header);

        if (!HasAnyRequiredScope(authContext, requiredScopes))
        {
            await ProblemResponse(
                context,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                "Insufficient scope for this operation."
            );
            return;
        }

        context.Items["AuthContext"] = authContext;

        await _next(context);
    }

    private static bool HasAnyRequiredScope(
        AuthContext caller,
        IReadOnlyList<string> requiredScopes
    )
    {
        return requiredScopes.Any(rs =>
            caller.Roles.Contains(rs, StringComparer.OrdinalIgnoreCase)
        );
    }

    private static string[] GetRequiredScopes(HttpContext context)
    {
        var endpoint = context.GetEndpoint();

        if (endpoint == null)
        {
            return [];
        }

        var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();

        if (actionDescriptor == null)
        {
            return [];
        }

        var methodInfo = actionDescriptor.MethodInfo;

        var requiredScopesAttribute = methodInfo.GetCustomAttribute<RequiredScopesAttribute>(false);

        return requiredScopesAttribute?.Scopes.ToArray() ?? [];
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

        return store;
    }

    private static async Task ProblemResponse(
        HttpContext context,
        HttpStatusCode code,
        string title,
        string detail
    )
    {
        await context.Response.WriteAsJsonAsync(
            TypedResults.Problem(detail, null, (int)code, title)
        );
    }
}
