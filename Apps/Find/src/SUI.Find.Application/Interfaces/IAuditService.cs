using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Interfaces;

public interface IAuditService
{
    Task WriteAccessAuditLogAsync(AuditAccessMessage accessMessage);
}
