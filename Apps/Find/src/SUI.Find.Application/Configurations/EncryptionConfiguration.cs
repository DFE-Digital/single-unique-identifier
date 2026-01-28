namespace SUI.Find.Application.Configurations;

public class EncryptionConfiguration
{
    // ! System critical value and the service should fall over if not found at startup
    public bool EnableGlobalPersonIdEncryption { get; init; }
}
