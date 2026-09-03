namespace SUI.GetAnIdentifier.Application.Interfaces;

public interface IAuditLogService
{
    Task LogIncomingRequestAsync(
        string callerId,
        string correlationId,
        DateTimeOffset timestamp,
        string httpMethod,
        string requestPath,
        object? requestData,
        CancellationToken cancellationToken = default
    );

    Task LogOutgoingResponseAsync(
        string callerId,
        string correlationId,
        DateTimeOffset timestamp,
        int statusCode,
        string responseSummary,
        CancellationToken cancellationToken = default
    );
}
