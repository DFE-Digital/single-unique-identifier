using SUI.Find.Domain.Events.Audit;

namespace SUI.Find.Application.Interfaces;

public interface IAuditQueueClient
{
    Task SendAuditEventAsync(AuditEvent auditMessage, CancellationToken cancellationToken);
}
