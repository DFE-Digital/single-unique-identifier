using SUI.Find.Domain.ValueObjects;
using SUI.Find.FindApi.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.Validators;

public static class StartSearchRequestValidator
{
    public static bool IsValid(StartSearchRequest? request, out string errorMessage)
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

        var nhsId = NhsPersonId.Create(request.Suid);
        if (!nhsId.Success)
        {
            errorMessage = nhsId.Error ?? "NHS Number is not valid";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
