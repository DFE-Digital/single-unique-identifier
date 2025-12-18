using System.Diagnostics.CodeAnalysis;
using SUI.SingleView.Application.Models;
using SUI.SingleView.Domain.Models;
using SUI.Transfer.API.Client;

namespace SUI.SingleView.Application.Services;

public class PersonMapper : IPersonMapper
{
    [ExcludeFromCodeCoverage(
        Justification = "Only stub data currently. Will be implemented very shortly in next PR."
    )]
    public PersonModel Map(string nhsNumber, ConformedData conformedData)
    {
        var personalDetails = conformedData.ConsolidatedData.PersonalDetailsRecord;
        var crimeData = conformedData.ConsolidatedData.CrimeDataRecord;

        var personName = $"{personalDetails?.FirstName?.Value} {personalDetails?.LastName?.Value}";
        personName = string.IsNullOrWhiteSpace(personName) ? "Unknown name" : personName;

        return new PersonModel
        {
            Name = personName,
            NhsNumber = nhsNumber,
            PersonalDetails = personalDetails,
            CrimeData = crimeData,
            Tags = (conformedData.StatusFlags?.Select(x => x.ToString()) ?? []).ToList(),
            ImportantMessages = [],
            PoliceMarker = !string.IsNullOrEmpty(crimeData?.PoliceMarkerDetails?.Value),
            PoliceMarkerDetails = crimeData?.PoliceMarkerDetails?.Value ?? "",
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
            EducationContactAddress = new Address
            {
                AddressLine1 = "Redwood Drive",
                AddressLine2 = "Maltby",
                Town = "London",
                Postcode = "SE6 8DL",
            },
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
            RegisteredGpName = "Dr E Smith",
            RegisteredGpContactAddress = new Address()
            {
                AddressLine1 = "Duke Medical Centre",
                AddressLine2 = "28 Talbot Road",
                Town = "Sheffield",
                Postcode = "S2 2TD",
            },
            RegisteredGpContactPhone = "0114 272 2100",
            CamhsContactPhone = "01422 345926",
            ActiveHealthPlans = [new ActivePlan { Name = "CAMHS plan", Status = "Open to CAMHS" }],
            HealthAttendanceSummary12Month = new Tuple<int, int, int, int>(3, 0, 0, 1),
            HealthAttendanceSummary5Year = new Tuple<int, int, int, int>(8, 0, 0, 3),
            ServicesKnownTo = ["Youth justice service (YJS)", "Police"],
            LastPoliceProtectionPowerEvent = "none",
            MissingEpisodesLast6Months = 5,
            SummaryOfRiskLast5Years =
            [
                "Criminal exploitation",
                "Radicalisation",
                "Gangs and Youth violence",
            ],
            ActiveCrimePlans =
            [
                new ActivePlan { Name = "-", Status = "Open to Youth justice service" },
            ],
        };
    }
}
