using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using SUI.Find.Domain.Models;
using SUI.Find.FindApi.Factories;
using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.Middleware;

[ExcludeFromCodeCoverage(Justification = "Middleware - covered by integration tests")]
// ReSharper disable once ClassNeverInstantiated.Global
public class AuditMiddleware(
    ILogger<AuditMiddleware> logger,
    IQueueClientFactory queueClientFactory
) : IFunctionsWorkerMiddleware
{
    private const string SwaggerPathSegment = "swagger";
    private const string TokenPathSegment = "auth/token";
    private const string ExactSearchesEndpoint = "/api/v1/searches";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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
        var authContext = context.Items.TryGetValue(nameof(AuthContext), out var contextItem)
            ? contextItem as AuthContext
            : null;

        if (authContext is not null)
        {
            var clientId = authContext.ClientId;

            var auditMessage = new AuditAccessMessage
            {
                ClientId = clientId,
                Path = httpReq.Url.AbsolutePath,
                Method = httpReq.Method,
                Timestamp = DateTime.UtcNow,
                CorrelationId = context.InvocationId,
                EventType = "Access",
            };

            var isSearchRequest = httpReq.Url.AbsolutePath.Equals(
                ExactSearchesEndpoint,
                StringComparison.OrdinalIgnoreCase
            );
            if (isSearchRequest)
            {
                await HandleSearchRequestAuditAsync(httpReq, context, auditMessage);
                // Very important: Reset the stream position for downstream middleware/functions
                httpReq.Body.Seek(0, SeekOrigin.Begin);
                await next(context);
                return;
            }

            await SendAuditMessage(auditMessage);
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

    private async Task SendAuditMessage(AuditAccessMessage auditMessage)
    {
        var queueClient = queueClientFactory.GetAuditClient();
        var messageJson = JsonSerializer.Serialize(auditMessage, JsonOptions);
        var auditMessageBytes = System.Text.Encoding.UTF8.GetBytes(messageJson);
        var base64Message = Convert.ToBase64String(auditMessageBytes);
        try
        {
            await queueClient.SendMessageAsync(base64Message);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send audit message for CorrelationId: {CorrelationId}",
                auditMessage.CorrelationId
            );
        }
    }

    private async Task HandleSearchRequestAuditAsync(
        HttpRequestData httpReq,
        FunctionContext context,
        AuditAccessMessage auditAccessMessage
    )
    {
        var requestBody = await httpReq.ReadAsStringAsync();
        var request = JsonSerializer.Deserialize<StartSearchRequest>(requestBody!, JsonOptions);

        if (request?.Suid is not null)
        {
            var withSuid = auditAccessMessage with { Suid = request.Suid };
            await SendAuditMessage(withSuid);
        }
        else
        {
            // Not an error, just an empty SUID
            logger.LogInformation(
                "SUID not found in search request for CorrelationId: {CorrelationId}",
                context.InvocationId
            );
            await SendAuditMessage(auditAccessMessage);
        }
    }
}
