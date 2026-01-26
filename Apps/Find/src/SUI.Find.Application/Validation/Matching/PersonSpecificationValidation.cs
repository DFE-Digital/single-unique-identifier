using System.Text.RegularExpressions;
using FluentValidation;
using SUI.Find.Application.Constants.Matching;
using SUI.Find.Application.Models.Matching;

namespace SUI.Find.Application.Validation.Matching;

public class PersonSpecificationValidation : AbstractValidator<PersonSpecification>
{
    public PersonSpecificationValidation()
    {
        RuleFor(x => x.Given)
            .NotEmpty()
            .MaximumLength(30) // Matches PDS Fhir max length
            .WithMessage(PersonValidationConstants.GivenNameInvalid);
        RuleFor(x => x.Family)
            .NotEmpty()
            .MaximumLength(30) // Matches PDS Fhir max length
            .WithMessage(PersonValidationConstants.FamilyNameInvalid);
        RuleFor(x => x.BirthDate)
            .NotEmpty()
            .WithMessage(PersonValidationConstants.BirthDateInvalid);
        RuleFor(x => x.Gender)
            .Must(BeAValidGender)
            .When(x => !string.IsNullOrEmpty(x.Gender))
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
        var regex = new Regex(
            "^(([A-Z][0-9]{1,2})|(([A-Z][A-HJ-Y][0-9]{1,2})|(([A-Z][0-9][A-Z])|([A-Z][A-HJ-Y][0-9]?[A-Z])))) [0-9][A-Z]{2}$",
            RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(250)
        );

        return regex.IsMatch(postcode!);
    }

    private static bool BeAValidGender(string? gender)
    {
        var validGenders = new[]
        {
            PdsConstants.Gender.Male,
            PdsConstants.Gender.Female,
            PdsConstants.Gender.Unknown,
            PdsConstants.Gender.Other,
        };
        return validGenders.Contains(gender);
    }

    private static bool BeValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        // Remove common separators
        var cleaned = Regex.Replace(phone, @"[\s\-\(\)]", "");

        // UK phone validation: supports local (10-11 digits) and international format
        var regex = new Regex(
            @"^(\+44\d{10}|0\d{9,10})$",
            RegexOptions.None,
            matchTimeout: TimeSpan.FromMilliseconds(250)
        );

        return regex.IsMatch(cleaned);
    }
}
