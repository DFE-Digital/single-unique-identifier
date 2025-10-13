using FluentValidation.Results;
using SUI.Find.Application.Models;
using SUI.Find.Application.Validation;
using SUI.Find.Domain.Enums;

namespace Sui.Find.Application.UnitTests.Validation;

public class DataQualityTranslatorTests
{
    private readonly PersonDataQualityTranslator _dataQualityTranslator = new();

    [Fact]
    public void Translate_ShouldReturnValid_WhenAllRequiredFieldsAreValid()
    {
        var spec = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.Parse("2000-01-01")
        };
        var validation = new ValidationResult();

        var (hasMetRequirements, result) = _dataQualityTranslator.Translate(spec, validation);

        Assert.True(hasMetRequirements);
        Assert.Equal(QualityType.Valid, result.Given);
        Assert.Equal(QualityType.Valid, result.Family);
        Assert.Equal(QualityType.Valid, result.BirthDate);
    }

    [Fact]
    public void Translate_ShouldReturnNotProvided_WhenRequiredFieldsAreMissing()
    {
        var spec = new PersonSpecification();
        var validation = new ValidationResult();

        var (hasMetRequirements, result) = _dataQualityTranslator.Translate(spec, validation);

        Assert.False(hasMetRequirements);
        Assert.Equal(QualityType.NotProvided, result.Given);
        Assert.Equal(QualityType.NotProvided, result.Family);
        Assert.Equal(QualityType.NotProvided, result.BirthDate);
    }

    [Fact]
    public void Translate_ShouldReturnInvalid_WhenValidationErrorsExist()
    {
        var spec = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.Parse("2000-01-01")
        };
        var validation = new ValidationResult([
            new ValidationFailure(nameof(PersonSpecification.Given), "Invalid"),
            new ValidationFailure(nameof(PersonSpecification.Family), "Invalid"),
            new ValidationFailure(nameof(PersonSpecification.BirthDate), "Invalid")
        ]);

        var (hasMetRequirements, result) = _dataQualityTranslator.Translate(spec, validation);

        Assert.False(hasMetRequirements);
        Assert.Equal(QualityType.Invalid, result.Given);
        Assert.Equal(QualityType.Invalid, result.Family);
        Assert.Equal(QualityType.Invalid, result.BirthDate);
    }

    [Fact]
    public void Translate_ShouldReturnNotProvided_WhenBirthDateIsNull()
    {
        var spec = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = null
        };
        var validation = new ValidationResult();

        var (hasMetRequirements, result) = _dataQualityTranslator.Translate(spec, validation);

        Assert.False(hasMetRequirements);
        Assert.Equal(QualityType.Valid, result.Given);
        Assert.Equal(QualityType.Valid, result.Family);
        Assert.Equal(QualityType.NotProvided, result.BirthDate);
    }

    [Fact]
    public void Translate_ShouldSetInvalidForOtherFields_WhenValidationErrorsExist()
    {
        var spec = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.Parse("2000-01-01"),
            Gender = "M",
            Phone = "123456789",
            Email = "john@example.com",
            AddressPostalCode = "12345"
        };
        var validation = new ValidationResult([
            new ValidationFailure(nameof(PersonSpecification.Gender), "Invalid"),
            new ValidationFailure(nameof(PersonSpecification.Phone), "Invalid"),
            new ValidationFailure(nameof(PersonSpecification.Email), "Invalid"),
            new ValidationFailure(nameof(PersonSpecification.AddressPostalCode), "Invalid")
        ]);

        var translator = new PersonDataQualityTranslator();
        var (_, result) = translator.Translate(spec, validation);

        Assert.Equal(QualityType.Invalid, result.Gender);
        Assert.Equal(QualityType.Invalid, result.Phone);
        Assert.Equal(QualityType.Invalid, result.Email);
        Assert.Equal(QualityType.Invalid, result.AddressPostalCode);
    }
    
    [Fact]
    public void Translate_ShouldSetNotProvidedForNonRequiredFields_WhenTheyAreNullOrEmpty()
    {
        var spec = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.Parse("2000-01-01"),
            Gender = null,
            Phone = null,
            Email = null,
            AddressPostalCode = null
        };
        var validation = new ValidationResult();

        var (_, result) = _dataQualityTranslator.Translate(spec, validation);

        Assert.Equal(QualityType.NotProvided, result.Gender);
        Assert.Equal(QualityType.NotProvided, result.Phone);
        Assert.Equal(QualityType.NotProvided, result.Email);
        Assert.Equal(QualityType.NotProvided, result.AddressPostalCode);
    }
}