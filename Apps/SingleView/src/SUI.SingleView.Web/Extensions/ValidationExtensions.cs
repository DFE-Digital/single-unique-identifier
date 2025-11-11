using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SUI.SingleView.Web.Extensions;

/// <summary>
/// Extension methods for Fluent Validation
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Adds the validation errors to the model state
    /// </summary>
    /// <param name="result">The validation result</param>
    /// <param name="modelState">The model state</param>
    /// <param name="prefix">The prefix to use for the model state keys</param>
    public static void AddToModelState(
        this ValidationResult result,
        ModelStateDictionary modelState,
        string? prefix = null
    )
    {
        foreach (var error in result.Errors)
        {
            var key = string.IsNullOrEmpty(prefix)
                ? error.PropertyName
                : $"{prefix}.{error.PropertyName}";

            modelState.AddModelError(key, error.ErrorMessage);
        }
    }
}
