namespace SUI.NotificationService.Application.Models;

/// <summary>
/// A single message read from the MESH mailbox.
/// </summary>
/// <param name="MessageId">MESH message identifier used to acknowledge the message once it has been processed.</param>
/// <param name="Content">The raw message body exactly as it was delivered to the mailbox: a FHIR Bundle.</param>
public sealed record MeshMailboxMessage(string MessageId, string Content);
