using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;

namespace SUI.Find.FindApi;

/// <summary>
/// Used for local development authentication via API Key header. Set in local.settings.json
/// For production, az functionapp config appsettings set in CI/CD pipelines
/// will be used to configure valid API keys
/// </summary>
/// <param name="config"></param>
// ReSharper disable once ClassNeverInstantiated.Global
public class AuthApiKeyMiddleware(IConfiguration config) : IFunctionsWorkerMiddleware
{
    private readonly Dictionary<string, string> _apiKeys =
        config
            .GetSection(Constants.Auth.ConfigurationKeysSection)
            .Get<List<ApiKeyConfig>>()
            ?.ToDictionary(k => k.Key, v => v.OrgId)
        ?? [];

    public async Task Invoke(FunctionContext ctx, FunctionExecutionDelegate next)
    {
        var httpReq = await ctx.GetHttpRequestDataAsync();
        if (httpReq is null)
        {
            await next(ctx);
            return;
        }

        if (!httpReq.Headers.TryGetValues(Constants.Auth.AuthHeaderApiKey, out var values))
        {
            await Reject(ctx, httpReq);
            return;
        }

        var providedKey = values.FirstOrDefault();
        if (providedKey is null || !_apiKeys.TryGetValue(providedKey, out var orgId))
        {
            await Reject(ctx, httpReq);
            return;
        }

        ctx.Items[Constants.Auth.OrgIdItemKey] = orgId;

        await next(ctx);
    }

    private static async Task Reject(FunctionContext ctx, HttpRequestData req)
    {
        var res = req.CreateResponse(HttpStatusCode.Unauthorized);
        await res.WriteStringAsync("Unauthorized");
        ctx.GetInvocationResult().Value = res;
    }
}

public record struct ApiKeyConfig(string Key, string OrgId);
