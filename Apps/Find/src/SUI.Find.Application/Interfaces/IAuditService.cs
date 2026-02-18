using SUI.Find.Domain.Events.Audit;

namespace SUI.Find.Application.Interfaces;

public interface IAuditService
{
    Task WriteAccessAuditLogAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
