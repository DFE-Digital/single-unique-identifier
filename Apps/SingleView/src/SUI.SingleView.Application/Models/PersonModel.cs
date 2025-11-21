using SUI.SingleView.Domain.Models;

namespace SUI.SingleView.Application.Models;

public record PersonModel
{
    // TODO: Group and break out these properties into their own structs/records
    //  once we know more about the API response structure

    public string Name { get; init; } = string.Empty;

    public string NhsNumber { get; init; } = string.Empty;

    public List<string> Tags { get; init; } = [];

    public List<string> ImportantMessages { get; init; } = [];

    public string SocialCareLastUpdated { get; init; } = string.Empty;

    public string EducationLastUpdated { get; init; } = string.Empty;

    public string HealthLastUpdated { get; init; } = string.Empty;

    public string CrimeLastUpdated { get; init; } = string.Empty;

    public string HousingLastUpdated { get; init; } = string.Empty;

    public string DateOfBirth { get; init; } = string.Empty;

    public string MainAddress { get; init; } = string.Empty;

    public bool PoliceMarker { get; init; }

    public string PoliceMarkerDetails { get; init; } = string.Empty;

    public List<string> IndividualsAtMainAddress { get; init; } = [];

    public string BirthAssignedSex { get; init; } = string.Empty;

    public string Pronouns { get; init; } = string.Empty;

    public string Ethnicity { get; init; } = string.Empty;

    public string FirstLanguage { get; init; } = string.Empty;

    public string DesignatedLocalAuthority { get; init; } = string.Empty;

    public string EnglishAsAdditionalLanguage { get; init; } = string.Empty;

    public string Braille { get; init; } = string.Empty;

    public string SignLanguage { get; init; } = string.Empty;

    public string Makaton { get; init; } = string.Empty;

    public string Interpreter { get; init; } = string.Empty;

    public List<Relationship> Relationships { get; init; } = [];

    public string KeyWorker { get; init; } = string.Empty;

    public string DutyContactEmail { get; init; } = string.Empty;

    public string DutyContactPhone { get; init; } = string.Empty;

    public List<string> TeamInvolvement { get; init; } = [];

    public List<ActivePlan> ActiveChildrensServicesPlans { get; init; } = [];

    public List<Tuple<string, int>> Referrals6Months { get; init; } = [];

    public List<Tuple<string, int>> Referrals12Months { get; init; } = [];

    public List<Tuple<string, int>> Referrals5Years { get; init; } = [];

    public string EducationSetting { get; init; } = string.Empty;

    public string EducationContactAddress { get; init; } = string.Empty;

    public string EducationContactPhone { get; init; } = string.Empty;

    public List<ActivePlan> ActiveEducationPlans { get; init; } = [];

    public string CurrentAcademicTermAttendance { get; init; } = string.Empty;

    public string CurrentAcademicTermUnauthorisedAbsence { get; init; } = string.Empty;

    public string CurrentAcademicTermSuspensions { get; init; } = string.Empty;

    public string CurrentAcademicTermExclusions { get; init; } = string.Empty;

    public string CurrentAcademicTermSchoolMoves { get; init; } = string.Empty;

    public string LastAcademicYearAttendance { get; init; } = string.Empty;

    public string LastAcademicYearUnauthorisedAbsence { get; init; } = string.Empty;

    public string LastAcademicYearSuspensions { get; init; } = string.Empty;

    public string LastAcademicYearExclusions { get; init; } = string.Empty;

    public string LastAcademicYearSchoolMoves { get; init; } = string.Empty;

    #region HealthProperties

    public string RegisteredGpName { get; init; } = string.Empty;

    public Address RegisteredGpContactAddress { get; init; } =
        new() { AddressLine1 = "No known address" };

    public string RegisteredGpContactPhone { get; init; } = string.Empty;

    public string CamhsContactPhone { get; init; } = string.Empty;

    public List<ActivePlan> ActiveHealthPlans { get; init; } = [];

    public Tuple<int, int, int, int> HealthAttendanceSummary12Month { get; init; } =
        new(0, 0, 0, 0);

    public Tuple<int, int, int, int> HealthAttendanceSummary5Year { get; init; } = new(0, 0, 0, 0);

    #endregion

    #region CrimeProperties

    public List<string> ServicesKnownTo { get; init; } = [];

    public string LastPoliceProtectionPowerEvent { get; init; } = string.Empty;

    public int MissingEpisodesLast6Months { get; init; } = 0;

    public List<string> SummaryOfRiskLast5Years { get; init; } = [];

    public List<ActivePlan> ActiveCrimePlans { get; init; } = [];

    #endregion
}
