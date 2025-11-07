namespace SUI.FakeCustodians.Application.Models
{
    public class PersonalData
    {
        public required string FirstName { get; init; }
    
        public required string LastName { get; init; }
    
        public DateTime DateOfBirth { get; init; }
    
        public string? NhsNumber { get; init; }
    }
}