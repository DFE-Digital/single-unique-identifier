using SUI.Find.FindApi.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.Validators;

public static class StartSearchRequestValidator
{
    public static bool IsValid(StartSearchRequest? request, out string? errorMessage)
    {
        if (request == null)
        {
            errorMessage = "Request cannot be null";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Suid))
        {
            errorMessage = "Suid is required";
            return false;
        }

        try
        {
            var bytes = PersonIdEncryptionService.Base64UrlDecodeNoPadding(request.Suid);
            if (bytes.Length != 16)
            {
                errorMessage = "Suid is invalid";
                return false;
            }
        }
        catch (FormatException)
        {
            errorMessage = "Suid is not valid Base64Url";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
