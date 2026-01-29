using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace SUI.Find.FindApi.Validators;

[ExcludeFromCodeCoverage(Justification = "Simple validation wrapper")]
public static class DataAnnotationValidator
{
    public static bool Validate<T>(T request, out string? message)
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
