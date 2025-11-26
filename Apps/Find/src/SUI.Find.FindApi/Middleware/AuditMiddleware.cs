using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.Middleware;

[ExcludeFromCodeCoverage(Justification = "Middleware - covered by integration tests")]
// ReSharper disable once ClassNeverInstantiated.Global
public class AuditMiddleware(IAuditService auditService, ILogger<AuditMiddleware> logger)
    : IFunctionsWorkerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var invocationId = context.InvocationId;
        logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = invocationId });
        {
            var httpReq = await context.GetHttpRequestDataAsync();
            if (httpReq is null)
            {
                await next(context);
                return;
            }

            // No requirement to audit swagger requests
            if (
                httpReq.Url.AbsolutePath.Contains(
                    "swagger",
                    StringComparison.CurrentCultureIgnoreCase
                )
            )
            {
                await next(context);
                return;
            }

            // All other endpoints should be audited
            var authContext = context.Items.TryGetValue("AuthContext", out var contextItem)
                ? contextItem as AuthContext
                : null;

            if (authContext is not null)
            {
                var clientId = authContext.ClientId;

                const string exactSearchesEndpoint = "/api/v1/searches";
                var isSearchRequest = httpReq.Url.AbsolutePath.Equals(
                    exactSearchesEndpoint,
                    StringComparison.OrdinalIgnoreCase
                );
                if (isSearchRequest)
                {
                    await HandleSearchRequestAuditAsync(httpReq, context, authContext.ClientId);
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
            }
            else
            {
                logger.LogWarning(
                    "No AuthContext found in FunctionContext items for CorrelationId: {CorrelationId}",
                    invocationId
                );
                await auditService.WriteAccessAuditLogAsync(
                    "UnknownClient",
                    httpReq.Url.AbsolutePath,
                    httpReq.Method,
                    DateTime.Now,
                    context.InvocationId
                );
            }

            await next(context);
        }
    }

    private async Task HandleSearchRequestAuditAsync(
        HttpRequestData httpReq,
        FunctionContext context,
        string clientId
    )
    {
        var requestBody = await httpReq.ReadAsStringAsync();
        var request = JsonSerializer.Deserialize<StartSearchRequest>(requestBody!, JsonOptions);

        if (request?.Suid is not null)
        {
            await auditService.WriteAccessWithSuidAuditLogAsync(
                clientId,
                httpReq.Url.AbsolutePath,
                httpReq.Method,
                request.Suid,
                DateTime.Now,
                context.InvocationId
            );
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
    }
}
