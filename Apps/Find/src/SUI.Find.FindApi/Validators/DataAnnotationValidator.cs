using System.ComponentModel.DataAnnotations;
using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.Validators;

public static class DataAnnotationValidator
{
    public static bool Validate(MatchPersonRequest request, out string? message)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);

        if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            message = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
            return false;
        }
        message = null;
        return true;
    }
}
