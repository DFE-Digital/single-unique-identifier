namespace SUI.NotificationService.Application;

public interface INotificationOrchestrator
{
    Task RunAsync(CancellationToken cancellationToken);
}
