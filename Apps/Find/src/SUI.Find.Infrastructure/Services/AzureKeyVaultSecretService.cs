using Azure;
using Azure.Security.KeyVault.Secrets;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.Infrastructure.Services;

public class AzureKeyVaultSecretService(SecretClient secretClient) : ISecretService
{
    /// <summary>
    /// Get a specified secret from a given key vault.
    /// </summary>
    /// <remarks>
    /// The get operation is applicable to any secret stored in Azure Key Vault.
    /// This operation requires the secrets/get permission.
    /// </remarks>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> controlling the request lifetime.</param>
    /// <exception cref="ArgumentException"><paramref name="secretName"/> is an empty string.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="secretName"/> is null.</exception>
    /// <exception cref="RequestFailedException">The server returned an error. See <see cref="Exception.Message"/> for details returned from the server.</exception>
    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken)
    {
        KeyVaultSecret secret = await secretClient.GetSecretAsync(
            secretName,
            cancellationToken: cancellationToken
        );
        return secret.Value;
    }
}
