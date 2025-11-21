namespace SUI.StubCustodians.Application.Contracts.Arbor
{
    public class ArborRecord : BaseEntity
    {
        public bool PupilPremium { get; init; }

        public bool FreeSchoolMeals { get; init; }

        public bool ElectivelyHomeEducated { get; init; }

        public IEnumerable<ArborSchool>? SchoolsAttended { get; init; }
    }
}
