using SUI.SingleView.Application.Models;

namespace SUI.SingleView.Application.Services;

public class RecordService : IRecordService
{
    public PersonModel GetRecord(string nhsNumber)
    {
        return new PersonModel
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
            Relationships =
            [
                new Relationship
                {
                    Name = "Jeff Middleton",
                    DateOfBirth = "1 November 1988 (37 years old)",
                    Risk = "Individual may possess firearms",
                    Type = "Father",
                    ServicesKnownTo = ["Police", "Probation", "Mental health"],
                },
                new Relationship
                {
                    Name = "Julie Middleton",
                    DateOfBirth = "29 February 1962 (59 years old)",
                    Risk = "Individual may resort to violent behaviour",
                    Type = "Birth mother",
                    ServicesKnownTo = ["Mental health", "Adult Social Care"],
                },
                new Relationship
                {
                    Name = "James Middleton",
                    DateOfBirth = "5 June 2012 (13 years old)",
                    Risk = "None",
                    Type = "Sibling",
                    ServicesKnownTo = ["Police", "Childrens Social Care"],
                },
                new Relationship
                {
                    Name = "Peter Middleton",
                    DateOfBirth = "20 July 2017 (8 years old)",
                    Risk = "None",
                    Type = "Sibling",
                    ServicesKnownTo = ["Childrens Social Care"],
                },
            ],
            KeyWorker = "Alex Patel",
            DutyContactEmail = "csc@bromley.gov.uk",
            DutyContactPhone = "08792675387",
            TeamInvolvement = ["Disabled Childrens Team", "Neighbourhood Team"],
            ActiveChildrensServicesPlans =
            [
                new ActivePlan { Name = "Child in need plan", Status = "Child In Need" },
            ],
            Referrals6Months =
            [
                new Tuple<string, int>("Early Help referrals", 8),
                new Tuple<string, int>("Social Care referrals", 10),
            ],
            Referrals12Months =
            [
                new Tuple<string, int>("Early Help referrals", 18),
                new Tuple<string, int>("Social Care referrals", 20),
            ],
            Referrals5Years =
            [
                new Tuple<string, int>("Early Help referrals", 25),
                new Tuple<string, int>("Social Care referrals", 38),
            ],
            EducationSetting = "Redwood Academy",
            EducationContactAddress = "Redwood Drive, Maltby, London, SE6 8DL",
            EducationContactPhone = "0123456789",
            ActiveEducationPlans =
            [
                new ActivePlan { Name = "Education, Health and Care (EHC) plan", Status = "SEND" },
            ],
            CurrentAcademicTermAttendance = "70",
            CurrentAcademicTermUnauthorisedAbsence = "2",
            CurrentAcademicTermSuspensions = "1",
            CurrentAcademicTermExclusions = "0",
            CurrentAcademicTermSchoolMoves = "0",
            LastAcademicYearAttendance = "78",
            LastAcademicYearUnauthorisedAbsence = "3",
            LastAcademicYearSuspensions = "2",
            LastAcademicYearExclusions = "0",
            LastAcademicYearSchoolMoves = "0",
        };
    }
}
