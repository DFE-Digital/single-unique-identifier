using System.ComponentModel.DataAnnotations;

namespace SUI.GetAnIdentifier.API.Configuration;

public class AbsoluteHttpsUriAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string stringValue || string.IsNullOrWhiteSpace(stringValue))
        {
            return false;
        }

        return Uri.TryCreate(stringValue, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps;
    }
}
