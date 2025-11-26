namespace SUI.Find.Application.Interfaces;

public interface IAuditService
{
    Task WriteAccessAuditLogAsync(
        string clientId,
        string url,
        string method,
        DateTime accessTime,
        string correlationId
    );

    Task WriteAccessWithSuidAuditLogAsync(
        string clientId,
        string url,
        string method,
        string suid,
        DateTime accessTime,
        string correlationId
    );
}
