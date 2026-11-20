namespace SUI.NotificationService.Application.Exceptions;

public class WebhookSecretRetrievalException : Exception
{
    public WebhookSecretRetrievalException(string message)
        : base(message) { }

    public WebhookSecretRetrievalException(string message, Exception innerException)
        : base(message, innerException) { }
}
