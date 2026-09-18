using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Webhooks;

public class SupplierWebhookDeliveryService(
    HttpClient httpClient,
    IWebhookSecretClient secretClient,
    TimeProvider timeProvider,
    ILogger<SupplierWebhookDeliveryService> logger
) : ISupplierWebhookDeliveryService
{
    private const string ContractMediaType =
        "application/vnd.dfe.sui.lifecycle-notification.v1+json";

    // Strict serialization: No pretty-printing, drop nulls, standard casing, and serialize enums to camelCase strings.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public async Task<WebhookDeliveryResult> DeliverAsync(
        WebhookDeliveryRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Note: NHS numbers and body contents are explicitly EXCLUDED from logs to satisfy Information Governance ACs.
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Initiating webhook delivery. DeliveryId: {DeliveryId}, EventId: {EventId}, Endpoint: {EndpointUrl}",
                    request.DeliveryId,
                    request.SourceEventId,
                    request.EndpointUrl
                );
            }

            // Construct Payload (SchemaVersion is automatically populated)
            var payload = new LifecycleNotification(
                EventId: request.SourceEventId,
                EventType: request.EventType,
                OccurredAt: request.OccurredAt.UtcDateTime,
                AffectedNhsNumber: request.AffectedNhsNumber
            );

            var bodyBytes = JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);

            // Retrieve Secret securely
            var base64Secret = await secretClient.GetSecretBase64Async(
                request.SecretKeyVaultReference,
                cancellationToken
            );
            var secretBytes = Convert.FromBase64String(base64Secret);

            // Generate Timestamps and Signature
            var timestampSeconds = timeProvider.GetUtcNow().ToUnixTimeSeconds();
            var signature = GenerateSignature(
                timestampSeconds,
                request.DeliveryId,
                bodyBytes,
                secretBytes
            );

            // Construct HTTP Request
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.EndpointUrl);

            var content = new ByteArrayContent(bodyBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(ContractMediaType);
            httpRequest.Content = content;

            httpRequest.Headers.Add("X-SUI-Delivery-ID", request.DeliveryId.ToString());
            httpRequest.Headers.Add(
                "X-SUI-Delivery-Attempt",
                request.DeliveryAttempt.ToString(CultureInfo.InvariantCulture)
            );
            httpRequest.Headers.Add(
                "X-SUI-Timestamp",
                timestampSeconds.ToString(CultureInfo.InvariantCulture)
            );
            httpRequest.Headers.Add("X-SUI-Key-ID", request.KeyId);
            httpRequest.Headers.Add("X-SUI-Signature-256", signature);
            httpRequest.Headers.Add("X-Correlation-ID", request.CorrelationId.ToString());

            // Dispatch Request
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            stopwatch.Stop();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Webhook delivery {DeliveryId} completed with status {StatusCode} in {Duration}ms.",
                    request.DeliveryId,
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds
                );
            }

            return new WebhookDeliveryResult(
                ResponseReceived: true,
                HttpStatusCode: (int)response.StatusCode,
                TimedOut: false,
                ConnectionFailed: false,
                Duration: stopwatch.Elapsed
            );
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The caller DID NOT cancel, meaning HttpClient timed out internally.
            stopwatch.Stop();
            logger.LogWarning(
                "Webhook delivery {DeliveryId} timed out after {Duration}ms.",
                request.DeliveryId,
                stopwatch.ElapsedMilliseconds
            );

            return new WebhookDeliveryResult(
                ResponseReceived: false,
                HttpStatusCode: null,
                TimedOut: true,
                ConnectionFailed: false,
                Duration: stopwatch.Elapsed
            );
        }
        // Note: If cancellationToken.IsCancellationRequested IS true, the exception naturally bubbles up here to the caller.
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            logger.LogWarning(
                "Webhook delivery {DeliveryId} suffered connection failure in {Duration}ms. Error: {Message}",
                request.DeliveryId,
                stopwatch.ElapsedMilliseconds,
                ex.Message
            );

            return new WebhookDeliveryResult(
                ResponseReceived: false,
                HttpStatusCode: (int?)ex.StatusCode,
                TimedOut: false,
                ConnectionFailed: true,
                Duration: stopwatch.Elapsed
            );
        }
    }

    private static string GenerateSignature(
        long timestamp,
        Guid deliveryId,
        byte[] bodyBytes,
        byte[] secretBytes
    )
    {
        var headerString = $"{timestamp.ToString(CultureInfo.InvariantCulture)}.{deliveryId}.";
        var headerBytes = Encoding.ASCII.GetBytes(headerString);

        // Combine exact raw bytes: ASCII headers + '.' + ASCII deliveryId + '.' + UTF8 body
        var signedBytes = new byte[headerBytes.Length + bodyBytes.Length];
        Buffer.BlockCopy(headerBytes, 0, signedBytes, 0, headerBytes.Length);
        Buffer.BlockCopy(bodyBytes, 0, signedBytes, headerBytes.Length, bodyBytes.Length);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(signedBytes);

        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
