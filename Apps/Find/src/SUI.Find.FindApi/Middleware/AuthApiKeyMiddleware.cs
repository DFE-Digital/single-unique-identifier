using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using SUI.Find.Infrastructure;

namespace SUI.Find.FindApi.Middleware;

/// <summary>
/// Used for local development authentication via API Key header. Set in local.settings.json
/// For azure deployment, add with az functionapp config appsettings set in CI/CD pipelines
/// </summary>
/// <param name="config"></param>
[ExcludeFromCodeCoverage(Justification = "Middleware for auth, testing via integration tests")]
// ReSharper disable once ClassNeverInstantiated.Global
public class AuthApiKeyMiddleware(IConfiguration config) : IFunctionsWorkerMiddleware
{
    private readonly Dictionary<string, string> _apiKeys =
        config
            .GetSection(FindApiConstants.Auth.ConfigurationKeysSection)
            .Get<List<ApiKeyConfig>>()
            ?.ToDictionary(k => k.Key, v => v.OrgId)
        ?? [];

    private static readonly string[] NonAuthPaths = ["/api/health"];

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpReq = await context.GetHttpRequestDataAsync();
        if (httpReq is null || RequiresNoAuth(httpReq))
        {
            await next(context);
            return;
        }

        if (!httpReq.Headers.TryGetValues(FindApiConstants.Auth.AuthHeaderApiKey, out var values))
        {
            await Reject(context, httpReq);
            return;
        }

        var providedKey = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            await Reject(context, httpReq);
            return;
        }

        // https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptographicoperations.fixedtimeequals?view=net-9.0
        foreach (
            var kvp in _apiKeys.Where(kvp =>
                CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(kvp.Key),
                    Encoding.UTF8.GetBytes(providedKey)
                )
            )
        )
        {
            context.Items[FindApiConstants.Auth.OrgIdItemKey] = kvp.Value;
            await next(context);
            return;
        }

        await Reject(context, httpReq);
    }

    private static async Task Reject(FunctionContext context, HttpRequestData req)
    {
        var res = req.CreateResponse(HttpStatusCode.Unauthorized);
        await res.WriteStringAsync("Unauthorized");
        context.GetInvocationResult().Value = res;
    }

    private static bool RequiresNoAuth(HttpRequestData req)
    {
        var path = req.Url.AbsolutePath.ToLowerInvariant();
        return NonAuthPaths.Any(path.StartsWith);
    }
}

public record struct ApiKeyConfig(string Key, string OrgId);
