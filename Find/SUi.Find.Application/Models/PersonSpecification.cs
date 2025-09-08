using FluentValidation;

namespace SUi.Find.Application.Models;

public class PersonSpecification
{
    public string? Given { get; set; }
    public string? Family { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressPostalCode { get; set; }
}

public class PersonSpecificationValidation : AbstractValidator<PersonSpecification>
{
    public PersonSpecificationValidation()
    {
        RuleFor(x => x.Given).NotEmpty().MinimumLength(20).WithMessage(PersonValidationConstants.GivenNameInvalid);
        RuleFor(x => x.Family).NotEmpty().MinimumLength(20).WithMessage(PersonValidationConstants.FamilyNameInvalid);
        RuleFor(x => x.BirthDate).NotEmpty();
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

        var regex = new System.Text.RegularExpressions.Regex(
            "^(([A-Z][0-9]{1,2})|(([A-Z][A-HJ-Y][0-9]{1,2})|(([A-Z][0-9][A-Z])|([A-Z][A-HJ-Y][0-9]?[A-Z])))) [0-9][A-Z]{2}$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

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

        var regex = new System.Text.RegularExpressions.Regex(@"^\+?[1-9]\d{1,14}$");
        return regex.IsMatch(phone);
    }
}

public static class PersonValidationConstants
{
    public const string GivenNameInvalid = "Given name cannot be greater than 20 characters";
    public const string FamilyNameInvalid = "Family name cannot be greater than 20 characters";
    public const string BirthDateInvalid = "Invalid date of birth";
    public const string GenderInvalid = "Gender has to match FHIR standards";
    public const string PhoneInvalid = "Invalid phone number.";
    public const string EmailInvalid = "Invalid email address.";
    public const string PostCodeInvalid = "Invalid postcode.";
}