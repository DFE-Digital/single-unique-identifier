namespace SUI.SingleView.Application.Models;

public class PersonModel
{
    public string Name { get; init; } = string.Empty;

    public string NhsNumber { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = [];

    public List<string> ImportantMessages { get; set; } = [];

    public string SocialCareLastUpdated { get; set; } = string.Empty;

    public string EducationLastUpdated { get; set; } = string.Empty;

    public string HealthLastUpdated { get; set; } = string.Empty;

    public string CrimeLastUpdated { get; set; } = string.Empty;

    public string HousingLastUpdated { get; set; } = string.Empty;

    public string DateOfBirth { get; set; } = string.Empty;

    public string MainAddress { get; set; } = string.Empty;

    public bool PoliceMarker { get; set; }

    public string PoliceMarkerDetails { get; set; } = string.Empty;

    public List<string> IndividualsAtMainAddress { get; set; } = [];

    public string BirthAssignedSex { get; set; } = string.Empty;

    public string Pronouns { get; set; } = string.Empty;

    public string Ethnicity { get; set; } = string.Empty;

    public string FirstLanguage { get; set; } = string.Empty;

    public string DesignatedLocalAuthority { get; set; } = string.Empty;

    public string EnglishAsAdditionalLanguage { get; set; } = string.Empty;

    public string Braille { get; set; } = string.Empty;

    public string SignLanguage { get; set; } = string.Empty;

    public string Makaton { get; set; } = string.Empty;

    public string Interpreter { get; set; } = string.Empty;
}
