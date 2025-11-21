using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;

namespace SUI.Find.FindApi;

/// <summary>
/// Used for local development authentication via API Key header. Set in local.settings.json
/// For azure deployment, add with az functionapp config appsettings set in CI/CD pipelines
/// </summary>
/// <param name="config"></param>
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
        if (providedKey is null || !_apiKeys.TryGetValue(providedKey, out var orgId))
        {
            await Reject(context, httpReq);
            return;
        }

        context.Items[FindApiConstants.Auth.OrgIdItemKey] = orgId;

        await next(context);
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
