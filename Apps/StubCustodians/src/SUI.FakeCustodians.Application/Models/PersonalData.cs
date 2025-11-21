namespace SUI.FakeCustodians.Application.Models
{
    public record PersonalData
    {
        public required string FirstName { get; init; }

        public required string LastName { get; init; }

        public DateTime DateOfBirth { get; init; }

        public string? NhsNumber { get; init; }
    }
}
