using System.Text.RegularExpressions;
using FluentValidation;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.Validation;

public class PersonSpecificationValidation : AbstractValidator<PersonSpecification>
{
    public PersonSpecificationValidation()
    {
        RuleFor(x => x.Given).NotEmpty().MaximumLength(30).WithMessage(PersonValidationConstants.GivenNameInvalid);
        RuleFor(x => x.Family).NotEmpty().MaximumLength(30).WithMessage(PersonValidationConstants.FamilyNameInvalid);
        RuleFor(x => x.BirthDate).NotEmpty().WithMessage(PersonValidationConstants.BirthDateInvalid);
        RuleFor(x => x.Gender)
            .Must(BeAValidGender)
            .WithMessage(PersonValidationConstants.GenderInvalid);
        RuleFor(x => x.Phone)
            .Must(BeValidPhone)
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage(PersonValidationConstants.PhoneInvalid);
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage(PersonValidationConstants.EmailInvalid);
        RuleFor(x => x.AddressPostalCode)
            .Must(BeAValidPostcode)
            .When(x => !string.IsNullOrEmpty(x.AddressPostalCode))
            .WithMessage(PersonValidationConstants.PostCodeInvalid);
    }

    private static bool BeAValidPostcode(string? postcode)
    {
        if (string.IsNullOrEmpty(postcode))
        {
            return true;
        }

        var regex = new Regex(
            "^(([A-Z][0-9]{1,2})|(([A-Z][A-HJ-Y][0-9]{1,2})|(([A-Z][0-9][A-Z])|([A-Z][A-HJ-Y][0-9]?[A-Z])))) [0-9][A-Z]{2}$",
            RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));

        return regex.IsMatch(postcode);
    }

    private static bool BeAValidGender(string? gender)
    {
        if (string.IsNullOrEmpty(gender))
        {
            return true;
        }

        var validGenders = new[] { "male", "female", "unknown", "other" };
        return validGenders.Contains(gender);
    }

    private static bool BeValidPhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone))
        {
            return true;
        }

        var regex = new Regex(@"^\+?[1-9]\d{1,14}$", RegexOptions.None , matchTimeout: TimeSpan.FromMilliseconds(250));
        return regex.IsMatch(phone);
    }
}