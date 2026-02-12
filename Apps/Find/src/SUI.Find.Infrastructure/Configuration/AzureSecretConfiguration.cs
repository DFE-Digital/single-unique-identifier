namespace SUI.Find.Infrastructure.Configuration;

public class AzureSecretConfiguration
{
    public const string SectionName = "KeyVault";
    public required string KeyVaultUri { get; init; }
}
