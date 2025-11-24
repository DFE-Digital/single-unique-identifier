namespace SUI.Find.Infrastructure;

public interface IAuditService
{
    Task WriteAccessAuditLogAsync(
        string orgId,
        string url,
        string method,
        DateTime accessTime,
        string correlationId
    );

    Task WriteSearchWithSuidAuditLogAsync(
        string orgId,
        string url,
        string method,
        string hashedValue,
        DateTime accessTime,
        string correlationId
    );
}
