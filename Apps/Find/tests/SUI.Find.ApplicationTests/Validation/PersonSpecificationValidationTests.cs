using FluentValidation.TestHelper;
using SUI.Find.Application.Models.Matching;
using SUI.Find.Application.Validation.Matching;

namespace SUI.Find.ApplicationTests.Validation;

public class PersonSpecificationValidationTests
{
    private readonly PersonSpecificationValidation _validator = new();

    [Fact]
    public void Should_Pass_When_MinimalValidInput()
    {
        var model = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(1990, 1, 1),
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Given_IsEmpty()
    {
        var model = new PersonSpecification
        {
            Given = "",
            Family = "Doe",
            BirthDate = new DateOnly(1990, 1, 1),
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Given);
    }

    [Fact]
    public void Should_Fail_When_Family_IsEmpty()
    {
        var model = new PersonSpecification
        {
            Given = "John",
            Family = "",
            BirthDate = new DateOnly(1990, 1, 1),
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Family);
    }

    [Fact]
    public void Should_Fail_When_BirthDate_IsNull()
    {
        var model = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = null,
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.BirthDate);
    }

    [Theory]
    [InlineData("male")]
    [InlineData("female")]
    [InlineData("unknown")]
    [InlineData("other")]
    [InlineData(null)]
    [InlineData("")]
    public void Should_Pass_When_Gender_IsValidOrEmpty(string? gender)
    {
        var model = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(1990, 1, 1),
            Gender = gender,
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public void Should_Fail_When_Gender_IsInvalid()
    {
        var model = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(1990, 1, 1),
            Gender = "invalid",
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Gender);
    }

    [Theory]
    [InlineData("+1234567890")]
    [InlineData("1234567890")]
    [InlineData(null)]
    [InlineData("")]
    public void Should_Pass_When_Phone_IsValidOrEmpty(string? phone)
    {
        var model = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(1990, 1, 1),
            Phone = phone,
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("+0")]
    [InlineData("+12345678901234567")]
    public void Should_Fail_When_Phone_IsInvalid(string phone)
    {
        var model = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(1990, 1, 1),
            Phone = phone,
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData(null)]
    [InlineData("")]
    public void Should_Pass_When_Email_IsValidOrEmpty(string? email)
    {
        var model = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(1990, 1, 1),
            Email = email,
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    public void Should_Fail_When_Email_IsInvalid(string email)
    {
        var model = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(1990, 1, 1),
            Email = email,
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("SW1A 1AA")]
    [InlineData("EC1A 1BB")]
    [InlineData(null)]
    [InlineData("")]
    public void Should_Pass_When_Postcode_IsValidOrEmpty(string? postcode)
    {
        var model = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = postcode,
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.AddressPostalCode);
    }

    [Theory]
    [InlineData("SW1A1AA")]
    [InlineData("12345")]
    [InlineData("AAAA AAA")]
    public void Should_Fail_When_Postcode_IsInvalid(string postcode)
    {
        var model = new PersonSpecification
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(1990, 1, 1),
            AddressPostalCode = postcode,
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.AddressPostalCode);
    }

    [Fact]
    public void Should_Fail_When_Given_ExceedsMaxLength()
    {
        var model = new PersonSpecification
        {
            Given = new string('a', 31),
            Family = "Doe",
            BirthDate = new DateOnly(1990, 1, 1),
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Given);
    }

    [Fact]
    public void Should_Fail_When_Family_ExceedsMaxLength()
    {
        var model = new PersonSpecification
        {
            Given = "John",
            Family = new string('a', 31),
            BirthDate = new DateOnly(1990, 1, 1),
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Family);
    }
}
