using FluentValidation.Results;
using SUI.Matching.Application.Models;
using SUI.Matching.Application.Validation;

namespace SUI.Matching.Application.Interfaces;

public interface IDataQualityTranslator
{
    (bool hasMetRequirements, DataQualityResult dataQualityResult) Translate(PersonSpecification spec, ValidationResult validation);
}