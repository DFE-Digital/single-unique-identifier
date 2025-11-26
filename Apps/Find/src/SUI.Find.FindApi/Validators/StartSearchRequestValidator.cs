using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.Validators;

public class StartSearchRequestValidator
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
            errorMessage = "NHS number is required";
            return false;
        }

        var nhsNumber = request.Suid.Replace(" ", "");

        if (nhsNumber.Length != 10)
        {
            errorMessage = "NHS number must be 10 digits";
            return false;
        }

        if (!long.TryParse(nhsNumber, out _))
        {
            errorMessage = "NHS number must contain only digits";
            return false;
        }

        if (!IsValidNhsNumberChecksum(nhsNumber))
        {
            errorMessage = "NHS number checksum is invalid";
            return false;
        }

        errorMessage = null;
        return true;
    }

    public static bool IsValidNhsNumberChecksum(string nhsNumber)
    {
        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            sum += (nhsNumber[i] - '0') * (10 - i);
        }

        var checkDigit = 11 - (sum % 11);
        if (checkDigit == 11)
        {
            checkDigit = 0;
        }

        if (checkDigit == 10)
        {
            return false;
        }

        return checkDigit == (nhsNumber[9] - '0');
    }
}
