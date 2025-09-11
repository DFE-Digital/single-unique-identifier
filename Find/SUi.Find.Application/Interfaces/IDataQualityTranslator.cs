using FluentValidation.Results;
using SUi.Find.Application.Models;
using SUi.Find.Application.Validation;

namespace SUi.Find.Application.Interfaces;

public interface IDataQualityTranslator
{
    (bool hasMetRequirements, DataQualityResult dataQualityResult) Translate(PersonSpecification spec, ValidationResult validation);
}