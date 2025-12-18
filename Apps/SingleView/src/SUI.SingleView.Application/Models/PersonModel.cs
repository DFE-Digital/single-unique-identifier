using SUI.SingleView.Domain.Models;
using SUI.Transfer.API.Client;

namespace SUI.SingleView.Application.Models;

public record PersonModel
{
    public string Name { get; init; } = string.Empty;

    public PersonalDetailsRecordV1Consolidated? PersonalDetails { get; init; }

    public ChildrensServicesDetailsRecordV1Consolidated? ChildrensServicesDetails { get; set; }

    public CrimeDataRecordV1Consolidated? CrimeData { get; init; }

    public string NhsNumber { get; init; } = string.Empty;

    public List<string> Tags { get; init; } = [];

    public List<string> ImportantMessages { get; init; } = [];

    public string SocialCareLastUpdated { get; init; } = string.Empty;

    public string EducationLastUpdated { get; init; } = string.Empty;

    public string HealthLastUpdated { get; init; } = string.Empty;

    public string CrimeLastUpdated { get; init; } = string.Empty;

    public string HousingLastUpdated { get; init; } = string.Empty;

    public bool PoliceMarker { get; init; }

    public string PoliceMarkerDetails { get; init; } = string.Empty;

    public List<ActivePlan> ActiveChildrensServicesPlans { get; init; } = [];

    public ChildServicesReferralSummaries? ChildServicesReferralSummaries { get; init; }

    public EducationDetailsRecordV1Consolidated? EducationDetails { get; init; }

    public EducationAttendanceSummaries? EducationAttendanceSummaries { get; init; }

    public List<ActivePlan> ActiveEducationPlans { get; init; } = [];

    #region HealthProperties

    public HealthDataRecordV1Consolidated? HealthData { get; init; }

    public List<ActivePlan> ActiveHealthPlans { get; init; } = [];

    public HealthAttendanceSummaries? HealthAttendanceSummaries { get; init; }

    #endregion

    #region CrimeProperties

    public List<string> ServicesKnownTo { get; init; } = [];

    public string LastPoliceProtectionPowerEvent { get; init; } = string.Empty;

    public int MissingEpisodesLast6Months { get; init; } = 0;

    public List<string> SummaryOfRiskLast5Years { get; init; } = [];

    public List<ActivePlan> ActiveCrimePlans { get; init; } = [];

    #endregion
}
