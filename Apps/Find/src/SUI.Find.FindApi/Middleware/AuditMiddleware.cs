using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using SUI.Find.FindApi.Models;
using SUI.Find.Infrastructure;

namespace SUI.Find.FindApi.Middleware;

[ExcludeFromCodeCoverage(Justification = "Middleware - covered by integration tests")]
// ReSharper disable once ClassNeverInstantiated.Global
public class AuditMiddleware(IAuditService auditService) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpReq = await context.GetHttpRequestDataAsync();
        if (httpReq is null)
        {
            await next(context);
            return;
        }

        var orgId =
            context.Items.TryGetValue(FindApiConstants.Auth.OrgIdItemKey, out var orgIdObj)
            && orgIdObj is string id
            && !string.IsNullOrWhiteSpace(id)
                ? id
                : "Unknown";

        const string exactSearchesEndpoint = "/api/v1/searches";
        var isSearchRequest = httpReq.Url.AbsolutePath.Equals(
            exactSearchesEndpoint,
            StringComparison.OrdinalIgnoreCase
        );
        if (isSearchRequest)
        {
            await HandleSearchRequestAuditAsync(httpReq, context, orgId);
            // Very important: Reset the stream position for downstream middleware/functions
            httpReq.Body.Seek(0, SeekOrigin.Begin);
        }
        else
        {
            await auditService.WriteAccessAuditLogAsync(
                orgId,
                httpReq.Url.AbsolutePath,
                httpReq.Method,
                DateTime.Now,
                context.InvocationId
            );
        }

        await next(context);
    }

    private async Task HandleSearchRequestAuditAsync(
        HttpRequestData httpReq,
        FunctionContext context,
        string orgId
    )
    {
        var requestBody = await httpReq.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(requestBody!);
        if (
            doc.RootElement.TryGetProperty(
                nameof(StartSearchRequest.Suid).ToLower(),
                out var suidElement
            )
        )
        {
            var suid = suidElement.GetString() ?? string.Empty;
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(suid));
            var hashedSuid = Convert.ToHexString(hashBytes);

            await auditService.WriteSearchWithSuidAuditLogAsync(
                orgId,
                httpReq.Url.AbsolutePath,
                httpReq.Method,
                hashedSuid,
                DateTime.Now,
                context.InvocationId
            );
        }
    }
}
