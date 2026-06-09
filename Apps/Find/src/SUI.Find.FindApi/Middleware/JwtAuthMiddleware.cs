using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using SUI.Find.Application.Constants;
using SUI.Find.FindApi.Attributes;
using SUI.Find.FindApi.Configurations;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Utility;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.Middleware;

public class JwtAuthMiddleware(
    IAuthStoreService authStoreService,
    IAuthContextFactory authContextFactory,
    IConfigurationManager<OpenIdConnectConfiguration> oidcConfigManager,
    IOptions<AuthSettings> authSettings,
    ILogger<JwtAuthMiddleware> logger
) : IFunctionsWorkerMiddleware
{
    // One concrete handler with no interfaces, no injection mocks.
    private static readonly JwtSecurityTokenHandler TokenHandler = new();

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
        JwtSecurityToken jwt;

        try
        {
            var unverifiedToken = TokenHandler.ReadJwtToken(token);
            var algorithm = unverifiedToken.Header.Alg;
            SecurityToken validatedToken;

            if (algorithm == SecurityAlgorithms.RsaSha256)
            {
                validatedToken = await ValidateAsymmetricTokenAsync(
                    token,
                    unverifiedToken,
                    context.CancellationToken
                );
            }
            else if (algorithm == SecurityAlgorithms.HmacSha256)
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = store.Issuer,
                    ValidateAudience = true,
                    ValidAudience = store.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(store.SigningKey)
                    ),
                    ValidateLifetime = true,
                };

                // Uses the concrete .NET framework validation engine directly
                TokenHandler.ValidateToken(token, validationParameters, out validatedToken);
            }
            else
            {
                context.GetInvocationResult().Value = await HttpResponseUtility.ProblemResponse(
                    req,
                    HttpStatusCode.Unauthorized,
                    nameof(HttpStatusCode.Unauthorized),
                    $"Unsupported signing algorithm '{algorithm}'."
                );
                return;
            }

            jwt = (JwtSecurityToken)validatedToken;
        }
        catch (SecurityTokenException ex)
        {
            logger.LogWarning(ex, "Token validation failed");
            context.GetInvocationResult().Value = await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                nameof(HttpStatusCode.Unauthorized),
                "Token validation failed."
            );
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Invalid bearer token");
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

    private async Task<SecurityToken> ValidateAsymmetricTokenAsync(
        string token,
        JwtSecurityToken unverifiedToken,
        CancellationToken cancellationToken,
        bool allowRetry = true
    )
    {
        var oidcConfig = await oidcConfigManager.GetConfigurationAsync(cancellationToken);
        var configValues = authSettings.Value;

        var validationParameters = new TokenValidationParameters
        {
            RequireSignedTokens = true,
            ValidateIssuer = true,
            ValidIssuer = configValues.Issuer,
            ValidateAudience = true,
            ValidAudience = configValues.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = oidcConfig.SigningKeys,
            ValidateLifetime = true,
        };

        try
        {
            TokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return validatedToken;
        }
        catch (SecurityTokenSignatureKeyNotFoundException)
        {
            if (
                allowRetry
                && unverifiedToken.Issuer == configValues.Issuer
                && unverifiedToken.Audiences.Contains(configValues.Audience)
            )
            {
                oidcConfigManager.RequestRefresh();
                return await ValidateAsymmetricTokenAsync(
                    token,
                    unverifiedToken,
                    cancellationToken,
                    allowRetry: false
                );
            }
            throw;
        }
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

    private bool HasAnyRequiredScope(AuthContext caller, IReadOnlyList<string> requiredScopes)
    {
        if (authSettings.Value.UseAuthStoreForAuthorisation)
        {
            return requiredScopes.Any(rs =>
                authStoreService
                    .GetScopesByClientId(caller.ClientId)
                    .Contains(rs, StringComparer.OrdinalIgnoreCase)
            );
        }

        return requiredScopes.Any(rs =>
            caller.Scopes.Contains(rs, StringComparer.OrdinalIgnoreCase)
        );
    }
}
