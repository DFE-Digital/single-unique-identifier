using SUI.SingleView.Domain.Models;

namespace SUI.SingleView.Application.Models;

public class PersonModel
{
    // TODO: Group and break out these properties into their own structs/records
    //  once we know more about the API response structure

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

    public List<Relationship> Relationships { get; set; } = [];

    public string KeyWorker { get; set; } = string.Empty;

    public string DutyContactEmail { get; set; } = string.Empty;

    public string DutyContactPhone { get; set; } = string.Empty;

    public List<string> TeamInvolvement { get; set; } = [];

    public List<ActivePlan> ActiveChildrensServicesPlans { get; set; } = [];

    public List<Tuple<string, int>> Referrals6Months { get; set; } = [];

    public List<Tuple<string, int>> Referrals12Months { get; set; } = [];

    public List<Tuple<string, int>> Referrals5Years { get; set; } = [];

    public string EducationSetting { get; set; } = string.Empty;

    public string EducationContactAddress { get; set; } = string.Empty;

    public string EducationContactPhone { get; set; } = string.Empty;

    public List<ActivePlan> ActiveEducationPlans { get; set; } = [];

    public string CurrentAcademicTermAttendance { get; set; } = string.Empty;

    public string CurrentAcademicTermUnauthorisedAbsence { get; set; } = string.Empty;

    public string CurrentAcademicTermSuspensions { get; set; } = string.Empty;

    public string CurrentAcademicTermExclusions { get; set; } = string.Empty;

    public string CurrentAcademicTermSchoolMoves { get; set; } = string.Empty;

    public string LastAcademicYearAttendance { get; set; } = string.Empty;

    public string LastAcademicYearUnauthorisedAbsence { get; set; } = string.Empty;

    public string LastAcademicYearSuspensions { get; set; } = string.Empty;

    public string LastAcademicYearExclusions { get; set; } = string.Empty;

    public string LastAcademicYearSchoolMoves { get; set; } = string.Empty;

    #region HealthProperties

    public string RegisteredGpName { get; set; } = string.Empty;

    public Address RegisteredGpContactAddress { get; set; } =
        new() { AddressLine1 = "No known address" };

    public string RegisteredGpContactPhone { get; set; } = string.Empty;

    public string CamhsContactPhone { get; set; } = string.Empty;

    public List<ActivePlan> ActiveHealthPlans { get; set; } = [];

    public Tuple<int, int, int, int> HealthAttendanceSummary12Month { get; set; } = new(0, 0, 0, 0);

    public Tuple<int, int, int, int> HealthAttendanceSummary5Year { get; set; } = new(0, 0, 0, 0);

    #endregion
}
