using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.Validators;

public static class StartSearchRequestValidator
{
    private const int AesBlockSize = 16;
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

        if (request.Suid.Length != AesBlockSize)
        {
            errorMessage = $"Suid must be exactly {AesBlockSize} characters";
            return false;
        }
        
        errorMessage = null;
        return true;
    }
    
}
