using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.IdentityModel.Tokens;
using SUI.Find.CustodianSimulation.Models;

namespace SUI.Find.CustodianSimulation.Services;

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class ScopeEnforcementService : IFunctionsWorkerMiddleware
{
    private static readonly JwtSecurityTokenHandler Handler = new();
    private static readonly Lazy<AuthStore> Store = new(LoadAuthStore);
    private static readonly Assembly Assembly = typeof(ScopeEnforcementService).Assembly;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var req = await context.GetHttpRequestDataAsync();

        if (req is null)
        {
            await next(context);
            return;
        }

        var requiredScopes = GetRequiredScopes(context);

        if (requiredScopes.Count == 0)
        {
            await next(context);
            return;
        }

        if (!req.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            context.GetInvocationResult().Value = await ProblemResponse(
                req,
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
            context.GetInvocationResult().Value = await ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                "Missing or invalid bearer token."
            );
            return;
        }

        var token = header["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            context.GetInvocationResult().Value = await ProblemResponse(
                req,
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
            context.GetInvocationResult().Value = await ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                ex.Message
            );
            return;
        }
        catch
        {
            context.GetInvocationResult().Value = await ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                "Invalid bearer token."
            );
            return;
        }

        var authContext = AuthContextFactory.FromAuthorizationHeader(header);

        if (!HasAnyRequiredScope(authContext, requiredScopes))
        {
            context.GetInvocationResult().Value = await ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                "Unauthorised",
                "Insufficient scope for this operation."
            );
            return;
        }

        context.Items["AuthContext"] = authContext;

        await next(context);
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

    private static IReadOnlyList<string> GetRequiredScopes(FunctionContext context)
    {
        var entryPoint = context.FunctionDefinition.EntryPoint;

        if (string.IsNullOrWhiteSpace(entryPoint))
        {
            return [];
        }

        var lastDot = entryPoint.LastIndexOf('.');
        if (lastDot < 0 || lastDot == entryPoint.Length - 1)
        {
            return [];
        }

        var typeName = entryPoint.Substring(0, lastDot);
        var methodName = entryPoint.Substring(lastDot + 1);

        var type = Assembly.GetType(typeName);
        if (type is null)
        {
            return [];
        }

        var method = type.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
        if (method is null)
        {
            return [];
        }

        var attr = method.GetCustomAttribute<RequiredScopesAttribute>();
        return attr?.Scopes?.ToArray() ?? [];
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
