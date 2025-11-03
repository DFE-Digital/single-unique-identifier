using FluentValidation.Results;
using SUI.Find.Application.Models;
using SUI.Find.Application.Validation;

namespace SUI.Find.Application.Interfaces;

public interface IDataQualityTranslator
{
    (bool hasMetRequirements, DataQualityResult dataQualityResult) Translate(PersonSpecification spec, ValidationResult validation);
}