using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using SUI.Matching.Application.Interfaces;

namespace SUI.Matching.Infrastructure.Services;

public class SecretService(ILogger<SecretService> logger,
SecretClient secretClient) : ISecretService
{
    public async Task<string> GetSecret(string secretName, CancellationToken cancellationToken)
    {
        try
        {
            KeyVaultSecret secret = await secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            return secret.Value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get secret from Key Vault for secret name '{SecretName}'.", secretName);
            throw new InvalidOperationException($"Failed to get secret: {secretName}");
        }
    }
}