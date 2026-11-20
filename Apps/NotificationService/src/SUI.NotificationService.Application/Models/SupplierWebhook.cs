namespace SUI.NotificationService.Application.Models;

public class SupplierWebhook
{
    public string SupplierId { get; }
    public string EndpointUrl { get; }
    public bool IsEnabled { get; private set; }
    public string ContractVersion { get; }
    public string SecretKeyVaultReference { get; }
    public string KeyId { get; }

    public SupplierWebhook(
        string supplierId,
        string endpointUrl,
        bool isEnabled,
        string contractVersion,
        string secretKeyVaultReference,
        string keyId
    )
    {
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            throw new ArgumentException("Supplier ID cannot be empty", nameof(supplierId));
        }

        if (string.IsNullOrWhiteSpace(contractVersion))
        {
            throw new ArgumentException(
                "Contract version cannot be empty",
                nameof(contractVersion)
            );
        }

        if (string.IsNullOrWhiteSpace(secretKeyVaultReference))
        {
            throw new ArgumentException(
                "Key Vault reference cannot be empty",
                nameof(secretKeyVaultReference)
            );
        }

        if (string.IsNullOrWhiteSpace(keyId))
        {
            throw new ArgumentException("Key ID cannot be empty", nameof(keyId));
        }

        if (
            !Uri.TryCreate(endpointUrl, UriKind.Absolute, out var uriResult)
            || uriResult.Scheme != Uri.UriSchemeHttps
        )
        {
            throw new ArgumentException(
                "Endpoint URL must be a valid HTTPS absolute URI",
                nameof(endpointUrl)
            );
        }

        SupplierId = supplierId;
        EndpointUrl = endpointUrl;
        IsEnabled = isEnabled;
        ContractVersion = contractVersion;
        SecretKeyVaultReference = secretKeyVaultReference;
        KeyId = keyId;
    }

    public void Disable()
    {
        IsEnabled = false;
    }

    public void Enable()
    {
        IsEnabled = true;
    }
}
