using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using SUI.Find.Domain.Models;

namespace SUI.Find.Domain.ValueObjects;

[ExcludeFromCodeCoverage]
public partial record EncryptedPersonId
{
    public string Value { get; }

    private const int RequiredLength = 22;
    private static readonly Regex regex = Base64UrlRegex();

    private EncryptedPersonId(string id)
    {
        Value = id;
    }

    public static Result<EncryptedPersonId> Create(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("EncryptedPersonId cannot be empty.");

        if (id.Length != RequiredLength)
        {
            return Result<EncryptedPersonId>.Fail(
                $"EncryptedPersonId must have length of {RequiredLength} characters."
            );
        }

        if (!regex.IsMatch(id))
        {
            return Result<EncryptedPersonId>.Fail(
                "EncryptedPersonId does not match expected format."
            );
        }

        return Result<EncryptedPersonId>.Ok(new EncryptedPersonId(id));
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.Compiled)]
    private static partial Regex Base64UrlRegex();
}
