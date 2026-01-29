using System.Diagnostics.CodeAnalysis;

namespace SUI.Find.Application.Configurations;

[ExcludeFromCodeCoverage]
public class EncryptionConfiguration
{
    public const string SectionName = "IdEncryption";

    // ! System critical value and the service should fall over if not found at startup
    public required bool EnableGlobalPersonIdEncryption { get; init; }
}
