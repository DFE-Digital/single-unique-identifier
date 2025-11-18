using Microsoft.AspNetCore.Mvc.RazorPages;
using SUI.SingleView.Application.Models;

namespace SUI.SingleView.Web.Pages;

public class DataView : PageModel
{
    private readonly ILogger<DataView> _logger;

    public PersonModel PersonModel { get; set; }

    public DataView(ILogger<DataView> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        PersonModel = new PersonModel
        {
            Name = "Test Person",
            NhsNumber = "1234567890",
            Tags =
            [
                "CHILD PROTECTION",
                "SPECIAL EDUCATIONAL NEEDS AND DISABILITIES",
                "OPEN TO CSC",
                "CHILD CRIMINAL EXPLOITATION",
            ],
            ImportantMessages =
            [
                "Risk of home visits - dangerous dog reported",
                "Domestic abuse victim in household",
            ],
            SocialCareLastUpdated = "11 July 2025",
            EducationLastUpdated = "10 November 2025",
            HealthLastUpdated = "12 September 2024",
            CrimeLastUpdated = "1 July 2025",
            HousingLastUpdated = "1 April 2024",
            DateOfBirth = "10 October 2011 (14 years old)",
            MainAddress = "72 Guild street, London, SE23 6FH",
            PoliceMarker = true,
            PoliceMarkerDetails = "Individuals at the address may resort to violent behaviour",
            IndividualsAtMainAddress =
            [
                "Jeff Middleton",
                "Peter Middleton",
                "James Middleton",
                "Jason Archer",
                "Sarah Flint-Smith",
            ],
            BirthAssignedSex = "Female",
            Pronouns = "She/Her",
            Ethnicity = "Irish Traveller",
            FirstLanguage = "English",
            DesignatedLocalAuthority = "Bromley",
            EnglishAsAdditionalLanguage = "No",
            Braille = "No",
            SignLanguage = "No",
            Makaton = "No",
            Interpreter = "No",
        };
    }
}
