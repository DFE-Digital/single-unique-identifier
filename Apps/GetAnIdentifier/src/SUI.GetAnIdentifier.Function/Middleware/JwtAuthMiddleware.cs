using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using SUI.Find.Application.Constants;
using SUI.GetAnIdentifier.Function.Configuration;
using SUI.GetAnIdentifier.Function.Utility;

namespace SUI.GetAnIdentifier.Function.Middleware;

public class JwtAuthMiddleware(
    IAuthContextFactory authContextFactory,
    IConfigurationManager<OpenIdConnectConfiguration> oidcConfigManager,
    IOptions<AuthSettings> authSettings,
    ILogger<JwtAuthMiddleware> logger
) : IFunctionsWorkerMiddleware
{
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

        JwtSecurityToken jwt;

        try
        {
            // Read the unverified token for passing into ValidateAsymmetricTokenAsync for the OIDC refresh logic.
            var unverifiedToken = TokenHandler.ReadJwtToken(token);

            // The underlying TokenHandler will only accept RSA.
            var validatedToken = await ValidateAsymmetricTokenAsync(
                token,
                unverifiedToken,
                context.CancellationToken
            );

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

        var authResult = authContextFactory.FromJwt(
            jwt,
            authSettings.Value.UseAuthStoreForAuthorisation
        );

        if (!authResult.IsSuccess)
        {
            logger.LogWarning("Access denied. Reason: {Reason}", authResult.ErrorMessage);

            context.GetInvocationResult().Value = await HttpResponseUtility.ProblemResponse(
                req,
                HttpStatusCode.Unauthorized,
                nameof(HttpStatusCode.Unauthorized),
                authResult.ErrorMessage
            );
            return;
        }

        context.Items[ApplicationConstants.Auth.AuthContextKey] = authResult.Context;

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
}
