namespace SUI.NotificationService.Application.Models;

public sealed record BroadcastAggregateResult
{
    public required Guid EventId { get; init; }

    // Keeping the MessageId so the Orchestrator knows which MESH message this belongs to
    public required string MessageId { get; init; }

    public required int TotalEnabledSuppliers { get; init; }

    public required int NewlySuccessful { get; init; }

    public required int FailedDeliveries { get; init; }

    public required int SkippedPreviouslyAcknowledged { get; init; }

    public required int EndpointsRequiringNewDeliveryAttempt { get; init; }

    public required IReadOnlyList<EndpointDeliveryResult> EndpointResults { get; init; }
}
