using System.Diagnostics.CodeAnalysis;

namespace SUI.Find.Domain.ValueObjects;

[ExcludeFromCodeCoverage]
public sealed record EncryptedPersonId(string EncryptedValue)
{
    public static EncryptedPersonId Create(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("EncryptedPersonId cannot be empty.");

        try
        {
            _ = Convert.FromBase64String(id);
        }
        catch
        {
            throw new ArgumentException("EncryptedPersonId must be valid Base64.");
        }

        return new EncryptedPersonId(id);
    }

    public override string ToString() => EncryptedValue;
}
