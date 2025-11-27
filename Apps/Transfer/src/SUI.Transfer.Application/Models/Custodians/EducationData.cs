namespace SUI.Transfer.Application.Models.Custodians;

public record EducationData : ICustodianRecord
{
    public bool PupilPremium { get; init; }

    public bool FreeSchoolMeals { get; init; }

    public bool ElectivelyHomeEducated { get; init; }

    public IEnumerable<School>? SchoolsAttended { get; init; }
}
