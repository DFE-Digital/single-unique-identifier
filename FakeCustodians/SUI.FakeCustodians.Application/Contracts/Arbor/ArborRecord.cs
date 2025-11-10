namespace SUI.FakeCustodians.Application.Contracts.Arbor
{
    public class ArborRecord
    {
        public required string FirstName { get; init; }

        public required string LastName { get; init; }

        public DateTime DateOfBirth { get; init; }

        public string? NhsNumber { get; init; }

        public bool PupilPremium { get; init; }

        public bool FreeSchoolMeals { get; init; }

        public bool ElectivelyHomeEducated { get; init; }

        public IEnumerable<ArborSchool>? SchoolsAttended { get; init; }
    }
}