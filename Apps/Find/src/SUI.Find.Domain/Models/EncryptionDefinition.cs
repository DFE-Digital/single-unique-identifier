namespace SUI.Find.Domain.Models;

public class EncryptionDefinition
{
    public string Algorithm { get; init; } = "AES-256-CBC";
    public string KeyId { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
}
