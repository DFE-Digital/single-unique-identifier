using FluentValidation.Results;
using SUi.Find.Application.Interfaces;
using SUi.Find.Application.Models;
using SUI.Find.Domain.Enums;

namespace SUi.Find.Application.Validation;

public class PersonDataQualityTranslator : IDataQualityTranslator
{
    public (bool hasMetRequirements, DataQualityResult dataQualityResult) Translate(PersonSpecification spec, ValidationResult validation)
    {
        var result = new DataQualityResult();

        foreach (var error in validation.Errors)
        {
            switch (error.PropertyName)
            {
                case nameof(PersonSpecification.Given):
                    result.Given = error.ErrorCode == "Required"
                        ? QualityType.NotProvided
                        : QualityType.Invalid;
                    break;

                case nameof(PersonSpecification.Family):
                    result.Family = error.ErrorCode == "Required"
                        ? QualityType.NotProvided
                        : QualityType.Invalid;
                    break;

                case nameof(PersonSpecification.BirthDate):
                    result.BirthDate = error.ErrorCode == "Required"
                        ? QualityType.NotProvided
                        : QualityType.Invalid;
                    break;

                case nameof(PersonSpecification.Gender):
                    result.Gender = QualityType.Invalid;
                    break;

                case nameof(PersonSpecification.Phone):
                    result.Phone = QualityType.Invalid;
                    break;

                case nameof(PersonSpecification.Email):
                    result.Email = QualityType.Invalid;
                    break;

                case nameof(PersonSpecification.AddressPostalCode):
                    result.AddressPostalCode = QualityType.Invalid;
                    break;
            }
        }

        // Properties with no validation errors but missing values
        if (string.IsNullOrEmpty(spec.Given)) result.Given = QualityType.NotProvided;
        if (string.IsNullOrEmpty(spec.Family)) result.Family = QualityType.NotProvided;
        if (string.IsNullOrEmpty(spec.BirthDate?.ToString())) result.BirthDate = QualityType.NotProvided;
        if (string.IsNullOrEmpty(spec.AddressPostalCode)) result.AddressPostalCode = QualityType.NotProvided;
        if (string.IsNullOrEmpty(spec.Email)) result.Email = QualityType.NotProvided;
        if (string.IsNullOrEmpty(spec.Gender)) result.Gender = QualityType.NotProvided;
        if (string.IsNullOrEmpty(spec.Phone)) result.Phone = QualityType.NotProvided;

        // Minimum is Given,Family,BirthDate all Valid
        var isValid = result is { Given: QualityType.Valid, Family: QualityType.Valid, BirthDate: QualityType.Valid };

        return (isValid, result);
    }
}