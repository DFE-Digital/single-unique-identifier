using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Models;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Validators;

namespace SUI.Find.FindApi.Middleware;

[ExcludeFromCodeCoverage(Justification = "Middleware - covered by integration tests")]
// ReSharper disable once ClassNeverInstantiated.Global
public class AuditMiddleware(
    IConfiguration config,
    IAuditService auditService,
    IPersonIdEncryptionService encryptionService,
    ILogger<AuditMiddleware> logger
) : IFunctionsWorkerMiddleware
{
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
            if (httpReq.Url.AbsolutePath.Contains("swagger"))
            {
                await next(context);
                return;
            }

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
        using var doc = JsonDocument.Parse(requestBody!);
        if (
            doc.RootElement.TryGetProperty(
                nameof(StartSearchRequest.Suid).ToLower(),
                out var suidElement
            )
        )
        {
            var suid = suidElement.GetString() ?? string.Empty;

            var validatedSuid = StartSearchRequestValidator.IsValidNhsNumberChecksum(suid);
            if (!validatedSuid)
            {
                logger.LogWarning("Invalid SUID format received for auditing");
                await auditService.WriteAccessAuditLogAsync(
                    clientId,
                    httpReq.Url.AbsolutePath,
                    httpReq.Method,
                    DateTime.Now,
                    context.InvocationId
                );
                return;
            }

            // Replace this with organisation specifics when we have them
            var keyForAuditEncryption = config["Audit:EncryptionKey"];
            var keyIdForAuditEncryption = config["Audit:IdKey"];
            if (keyForAuditEncryption is null || keyIdForAuditEncryption is null)
            {
                throw new InvalidOperationException("Audit encryption configuration is missing.");
            }

            var encryptionDefinition = new EncryptionDefinition
            {
                KeyId = keyIdForAuditEncryption,
                Key = keyForAuditEncryption,
            };
            var encryptedSui = encryptionService.EncryptNhsToPersonId(suid, encryptionDefinition);

            if (!encryptedSui.Success)
            {
                logger.LogWarning(
                    "Failed to encrypt SUID for auditing: {ErrorMessage}",
                    encryptedSui.Error
                );
                await auditService.WriteAccessAuditLogAsync(
                    clientId,
                    httpReq.Url.AbsolutePath,
                    httpReq.Method,
                    DateTime.Now,
                    context.InvocationId
                );
                return;
            }

            await auditService.WriteAccessWithSuidAuditLogAsync(
                clientId,
                httpReq.Url.AbsolutePath,
                httpReq.Method,
                encryptedSui.Value!,
                DateTime.Now,
                context.InvocationId
            );
        }
    }
}
