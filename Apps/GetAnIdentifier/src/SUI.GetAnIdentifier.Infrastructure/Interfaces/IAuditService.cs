using SUI.GetAnIdentifier.Infrastructure.Models;

namespace SUI.GetAnIdentifier.Infrastructure.Interfaces;

public interface IAuditService
{
    Task SendAuditEventAsync(AuditEvent entry);
}
