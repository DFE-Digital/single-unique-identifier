namespace SUI.NotificationService.Application.Models;

/// <summary>
/// A pds-record-change-2 notification that was read from the MESH mailbox and understood.
/// </summary>
/// <param name="MessageId">MESH message identifier, used to acknowledge the message once the change has been handled.</param>
/// <param name="NhsNumber">NHS number of the patient whose PDS record changed.</param>
public sealed record PdsRecordChangeNotification(string MessageId, string NhsNumber);
