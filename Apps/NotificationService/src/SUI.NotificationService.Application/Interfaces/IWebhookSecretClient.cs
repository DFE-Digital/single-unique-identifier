namespace SUI.NotificationService.Application.Interfaces;

public interface IWebhookSecretClient
{
    Task<string> GetSecretBase64Async(
        string keyVaultReference,
        CancellationToken cancellationToken = default
    );
}
