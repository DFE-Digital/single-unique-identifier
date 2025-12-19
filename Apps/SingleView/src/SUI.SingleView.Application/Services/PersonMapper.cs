using SUI.SingleView.Application.Models;
using SUI.SingleView.Domain.Extensions;
using SUI.Transfer.API.Client;

namespace SUI.SingleView.Application.Services;

public class PersonMapper : IPersonMapper
{
    public PersonModel Map(string nhsNumber, ConformedData conformedData)
    {
        var personalDetails = conformedData.ConsolidatedData.PersonalDetailsRecord;
        var childrensServicesDetails = conformedData
            .ConsolidatedData
            .ChildrensServicesDetailsRecord;
        var educationDetails = conformedData.ConsolidatedData.EducationDetailsRecord;
        var healthData = conformedData.ConsolidatedData.HealthDataRecord;
        var crimeData = conformedData.ConsolidatedData.CrimeDataRecord;

        var personName =
            $"{personalDetails?.FirstName?.Value} {personalDetails?.LastName?.Value}".Trim();
        personName = string.IsNullOrEmpty(personName) ? "Unknown name" : personName;

        return new PersonModel
        {
            Name = personName,
            NhsNumber = nhsNumber,

            PersonalDetails = personalDetails,
            ChildrensServicesDetails = childrensServicesDetails,
            EducationDetails = educationDetails,
            HealthData = healthData,
            CrimeData = crimeData,

            Tags = (
                conformedData.StatusFlags?.Select(flag => flag.ToFriendlyEnumString()) ?? []
            ).ToList(),
            PoliceMarker = !string.IsNullOrWhiteSpace(crimeData?.PoliceMarkerDetails?.Value),
            PoliceMarkerDetails = crimeData?.PoliceMarkerDetails?.Value?.Trim() ?? "",

            ChildServicesReferralSummaries = conformedData.ChildServicesReferralSummaries,
            EducationAttendanceSummaries = conformedData.EducationAttendanceSummaries,
            HealthAttendanceSummaries = conformedData.HealthAttendanceSummaries,

            MissingEpisodesLast6Months = conformedData
                .CrimeMissingEpisodesSummaries
                ?.Last6Months
                ?.Count,

            // TODO: SUI-1282:
            ImportantMessages = [],

            // TODO: SUI-1284:
            ActiveChildrensServicesPlans = [],
            ActiveEducationPlans = [],
            ActiveHealthPlans = [],
            ActiveCrimePlans = [],

            // TODO: Crime aggregations:
            ServicesKnownTo = [],
            SummaryOfRiskLast5Years = [],
        };
    }
}
