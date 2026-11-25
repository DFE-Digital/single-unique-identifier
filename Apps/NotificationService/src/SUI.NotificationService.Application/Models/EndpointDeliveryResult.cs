namespace SUI.NotificationService.Application.Models;

public sealed record EndpointDeliveryResult(string SupplierId, DeliveryOutcome Outcome);
