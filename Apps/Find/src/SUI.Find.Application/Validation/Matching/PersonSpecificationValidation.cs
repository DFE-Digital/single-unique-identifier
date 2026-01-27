using System.Text.RegularExpressions;
using FluentValidation;
using SUI.Find.Application.Constants.Matching;
using SUI.Find.Application.Models.Matching;

namespace SUI.Find.Application.Validation.Matching;

public partial class PersonSpecificationValidation : AbstractValidator<PersonSpecification>
{
    public PersonSpecificationValidation()
    {
        // PDS patient: https://digital.nhs.uk/developer/api-catalogue/personal-demographics-service-fhir#get-/Patient
        RuleFor(x => x.Given)
            .NotEmpty()
            .MaximumLength(35) // Matches PDS Fhir max length
            .WithMessage(PersonValidationConstants.GivenNameInvalid);
        RuleFor(x => x.Family)
            .NotEmpty()
            .MaximumLength(35) // Matches PDS Fhir max length
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
        if (string.IsNullOrWhiteSpace(postcode))
            return false;

        var cleaned = FindWhiteSpaceRegex().Replace(postcode, "");

        // UK postcode regex (handles all valid formats)
        var regex = new Regex(
            @"^[A-Z]{1,2}\d{1,2}[A-Z]?\d[A-Z]{2}$",
            RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(250)
        );

        return regex.IsMatch(cleaned);
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
        var cleaned = CommonPhoneSeparatorRegex().Replace(phone!, "");

        // UK phone validation: supports local (10-11 digits) and international format
        var regex = new Regex(
            @"^(\+44\d{10}|0\d{9,10})$",
            RegexOptions.None,
            matchTimeout: TimeSpan.FromMilliseconds(250)
        );

        return regex.IsMatch(cleaned);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex FindWhiteSpaceRegex();

    [GeneratedRegex(@"[\s\-\(\)]")]
    private static partial Regex CommonPhoneSeparatorRegex();
}
