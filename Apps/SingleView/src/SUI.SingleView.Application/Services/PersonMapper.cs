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
        var childrensServicesDetails = conformedData
            .ConsolidatedData
            .ChildrensServicesDetailsRecord;
        var educationDetails = conformedData.ConsolidatedData.EducationDetailsRecord;
        var healthData = conformedData.ConsolidatedData.HealthDataRecord;
        var crimeData = conformedData.ConsolidatedData.CrimeDataRecord;

        var personName = $"{personalDetails?.FirstName?.Value} {personalDetails?.LastName?.Value}";
        personName = string.IsNullOrWhiteSpace(personName) ? "Unknown name" : personName;

        return new PersonModel
        {
            Name = personName,
            NhsNumber = nhsNumber,
            PersonalDetails = personalDetails,
            ChildrensServicesDetails = childrensServicesDetails,
            CrimeData = crimeData,
            Tags = (conformedData.StatusFlags?.Select(x => x.ToString()) ?? []).ToList(),
            ImportantMessages = [], // TODO: SUI-1282
            PoliceMarker = !string.IsNullOrEmpty(crimeData?.PoliceMarkerDetails?.Value),
            PoliceMarkerDetails = crimeData?.PoliceMarkerDetails?.Value ?? "",
            ActiveChildrensServicesPlans = [], // TODO: SUI-1284
            ChildServicesReferralSummaries = conformedData.ChildServicesReferralSummaries,
            EducationDetails = educationDetails,
            EducationAttendanceSummaries = conformedData.EducationAttendanceSummaries,
            ActiveEducationPlans = [], // TODO: SUI-1284
            HealthData = healthData,
            ActiveHealthPlans = [], // TODO: SUI-1284
            HealthAttendanceSummaries = conformedData.HealthAttendanceSummaries,
            ServicesKnownTo = ["Youth justice service (YJS)", "Police"],
            LastPoliceProtectionPowerEvent = "none",
            MissingEpisodesLast6Months = 5,
            SummaryOfRiskLast5Years =
            [
                "Criminal exploitation",
                "Radicalisation",
                "Gangs and Youth violence",
            ],
            ActiveCrimePlans = [], // TODO: SUI-1284
        };
    }
}
