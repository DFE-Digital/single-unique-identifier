using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using SUI.Find.Application.Interfaces;
using SUI.Find.FindApi.Models;

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

        // No requirement to audit swagger requests
        if (httpReq.Url.AbsolutePath.Contains("swagger"))
        {
            await next(context);
            return;
        }

        var clientId = (context.Items["AuthContext"] as AuthContext)?.ClientId ?? "UnknownClient";

        const string exactSearchesEndpoint = "/api/v1/searches";
        var isSearchRequest = httpReq.Url.AbsolutePath.Equals(
            exactSearchesEndpoint,
            StringComparison.OrdinalIgnoreCase
        );
        if (isSearchRequest)
        {
            await HandleSearchRequestAuditAsync(httpReq, context, clientId);
            // Very important: Reset the stream position for downstream middleware/functions
            httpReq.Body.Seek(0, SeekOrigin.Begin);
        }
        else
        {
            await auditService.WriteAccessAuditLogAsync(
                clientId,
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
            // Change this to the AES encryption hash when available in future work.
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(suid));
            var hashedSuid = Convert.ToHexString(hashBytes);

            await auditService.WriteAccessWithSuidAuditLogAsync(
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
