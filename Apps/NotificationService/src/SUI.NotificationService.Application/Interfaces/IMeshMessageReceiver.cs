using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Application.Interfaces;

/// <summary>
/// Reads messages from an NHS MESH mailbox; each message body is a FHIR Bundle.
/// Implementations are responsible for transport concerns only; deciding when to read and when to
/// acknowledge belongs to orchestration.
/// </summary>
public interface IMeshMessageReceiver
{
    /// <summary>
    /// Returns the number of messages currently waiting in the MESH mailbox.
    /// </summary>
    Task<int> GetMessageCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the identifiers of the messages currently waiting in the MESH mailbox.
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
