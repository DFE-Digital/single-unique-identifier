using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Application.Interfaces;

/// <summary>
/// Reads messages from an NHS MESH mailbox; each message body is a FHIR Bundle.
/// Implementations are responsible for transport concerns only; deciding when to read and when to
/// acknowledge belongs to orchestration.
/// </summary>
public interface IMeshInboxClient
{
    /// <summary>
    /// Returns the identifiers of every message currently waiting in the MESH mailbox, following
    /// MESH's inbox paging so a mailbox holding more messages than fit in one response is reported
    /// in full.
    /// </summary>
    Task<IReadOnlyList<string>> GetMessageIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a single message without removing it from the MESH mailbox.
    /// </summary>
    Task<MeshMailboxMessage> ReadMessageAsync(
        string messageId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Confirms the message has been durably processed so it is removed from the MESH mailbox and not
    /// redelivered. Callers must only acknowledge after processing has succeeded.
    /// </summary>
    Task AcknowledgeMessageAsync(string messageId, CancellationToken cancellationToken = default);
}
