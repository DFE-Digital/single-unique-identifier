namespace SUi.Find.Application.Constants;

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