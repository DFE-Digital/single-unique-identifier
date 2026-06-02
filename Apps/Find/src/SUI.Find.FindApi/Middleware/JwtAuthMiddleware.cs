using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.IdentityModel.Tokens;
using SUI.Find.Application.Constants;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.Middleware;

[ExcludeFromCodeCoverage(
    Justification = "Waiting on Integration tests to cover middleware functionality."
)]
// ReSharper disable once ClassNeverInstantiated.Global
public class JwtAuthMiddleware(
    IAuthStoreService authStoreService,
    IAuthContextFactory authContextFactory
) : IFunctionsWorkerMiddleware
{
    private static readonly JwtSecurityTokenHandler Handler = new();

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var req = await context.GetHttpRequestDataAsync();

        if (req is null)
        {
            await next(context);
            return;
        }

        // allow swagger / openapi / health to work without auth
        if (
            req.Url.AbsolutePath.StartsWith("/api/swagger")
            || req.Url.AbsolutePath.StartsWith("/api/openapi")
            || req.Url.AbsolutePath.StartsWith("/api/v1/auth/token")
            || req.Url.AbsolutePath.StartsWith("/api/health")
        )
        {
            await next(context);
            return;
        }

        if (!req.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            context.GetInvocationResult().Value = await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                nameof(HttpStatusCode.Unauthorized),
                "Missing Authorization header."
            );
            return;
        }

        var bearer = authHeaders.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(bearer) || !bearer.StartsWith("Bearer "))
        {
            context.GetInvocationResult().Value = await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                nameof(HttpStatusCode.Unauthorized),
                "Invalid Authorization header."
            );
            return;
        }

        var token = bearer["Bearer ".Length..].Trim();

        var store = await authStoreService.GetAuthStoreAsync();

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

        JwtSecurityToken jwt;

        try
        {
            Handler.ValidateToken(token, validationParameters, out var validated);
            jwt = (JwtSecurityToken)validated;
        }
        catch (SecurityTokenException ex)
        {
            context.GetInvocationResult().Value = await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                nameof(HttpStatusCode.Unauthorized),
                ex.Message
            );
            return;
        }
        catch
        {
            context.GetInvocationResult().Value = await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                nameof(HttpStatusCode.Unauthorized),
                "Invalid bearer token."
            );
            return;
        }

        var authContext = authContextFactory.FromJwt(jwt, store);

        var requiredScopes = GetRequiredScopes(context);

        if (!HasAnyRequiredScope(authContext, requiredScopes))
        {
            context.GetInvocationResult().Value = await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                nameof(HttpStatusCode.Unauthorized),
                "Insufficient scope for this operation."
            );
            return;
        }

        context.Items[ApplicationConstants.Auth.AuthContextKey] = authContext;

        await next(context);
    }

    private static string[] GetRequiredScopes(FunctionContext context)
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

        var typeName = entryPoint[..lastDot];
        var methodName = entryPoint[(lastDot + 1)..];

        var type = Type.GetType(typeName);
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
        return attr?.Scopes.ToArray() ?? [];
    }

    private static bool HasAnyRequiredScope(
        AuthContext caller,
        IReadOnlyList<string> requiredScopes
    )
    {
        return requiredScopes.Any(rs =>
            caller.Scopes.Contains(rs, StringComparer.OrdinalIgnoreCase)
        );
    }
}
