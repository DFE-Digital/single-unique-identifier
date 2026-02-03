using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Events.Audit;
using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.Middleware;

[ExcludeFromCodeCoverage(Justification = "Middleware - covered by integration tests")]
// ReSharper disable once ClassNeverInstantiated.Global
public class AuditMiddleware(ILogger<AuditMiddleware> logger, IAuditQueueClient auditClient)
    : IFunctionsWorkerMiddleware
{
    private const string SwaggerPathSegment = "swagger";
    private const string TokenPathSegment = "auth/token";
    private const string ExactSearchesEndpoint = "/api/v1/searches";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var invocationId = context.InvocationId;
        logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = invocationId });
        var httpReq = await context.GetHttpRequestDataAsync();
        if (httpReq is null)
        {
            await next(context);
            return;
        }

        // No requirement to audit swagger requests
        var isSwaggerRequest = httpReq.Url.AbsolutePath.Contains(
            SwaggerPathSegment,
            StringComparison.CurrentCultureIgnoreCase
        );
        var isTokenRequest = httpReq.Url.AbsolutePath.Contains(
            TokenPathSegment,
            StringComparison.CurrentCultureIgnoreCase
        );
        if (isSwaggerRequest || isTokenRequest)
        {
            await next(context);
            return;
        }

        // All other endpoints should be audited
        var authContext = context.Items.TryGetValue(
            ApplicationConstants.Auth.AuthContextKey,
            out var contextItem
        )
            ? contextItem as AuthContext
            : null;

        if (authContext is not null)
        {
            var clientId = authContext.ClientId;

            var payload = new AuditAccessMessage
            {
                Path = httpReq.Url.AbsolutePath,
                Method = httpReq.Method,
            };

            var isSearchRequest = httpReq.Url.AbsolutePath.Equals(
                ExactSearchesEndpoint,
                StringComparison.OrdinalIgnoreCase
            );
            if (isSearchRequest)
            {
                var suid = await GetRequestSuid(httpReq);
                payload = payload with { Suid = suid };
                // Very important: Reset the stream position for downstream middleware/functions
                httpReq.Body.Seek(0, SeekOrigin.Begin);
            }

            var auditEvent = new AuditEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                CorrelationId = context.InvocationId,
                ServiceName = "AuditMiddleware",
                EventName = ApplicationConstants.Audit.HttpRequest.EventName,
                Actor = new AuditActor { ActorId = clientId, ActorRole = "Organisation" },
                Payload = JsonSerializer.SerializeToElement(payload),
            };

            await auditClient.SendAuditEventAsync(auditEvent, CancellationToken.None);
        }
        else
        {
            logger.LogError(
                "No AuthContext found in FunctionContext items for CorrelationId: {CorrelationId}",
                invocationId
            );
        }

        await next(context);
    }

    private static async Task<string> GetRequestSuid(HttpRequestData httpReq)
    {
        var requestBody = await httpReq.ReadAsStringAsync();
        var request = JsonSerializer.Deserialize<StartSearchRequest>(
            requestBody!,
            JsonSerializerOptions.Web
        );

        if (request?.Suid is not null)
        {
            return request.Suid;
        }

        return string.Empty;
    }
}
