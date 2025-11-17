namespace SUI.SingleView.Application.Models;

public class PersonModel
{
    public string Name { get; init; } = string.Empty;

    public string NhsNumber { get; set; }

    public List<string> Tags { get; set; }

    public List<string> ImportantMessages { get; set; }

    public string SocialCareLastUpdated { get; set; }

    public string EducationLastUpdated { get; set; }

    public string HealthLastUpdated { get; set; }

    public string CrimeLastUpdated { get; set; }

    public string HousingLastUpdated { get; set; }

    public string DateOfBirth { get; set; }

    public string MainAddress { get; set; }

    public bool PoliceMarker { get; set; }

    public string PoliceMarkerDetails { get; set; }

    public List<string> IndividualsAtMainAddress { get; set; }

    public string BirthAssignedSex { get; set; }

    public string Pronouns { get; set; }

    public string Ethnicity { get; set; }

    public string FirstLanguage { get; set; }

    public string DesignatedLocalAuthority { get; set; }

    public string EnglishAsAdditionalLanguage { get; set; }

    public string Braille { get; set; }

    public string SignLanguage { get; set; }

    public string Makaton { get; set; }

    public string Interpreter { get; set; }
}
