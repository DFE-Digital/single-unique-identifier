namespace SUI.FakeCustodians.Application.Models
{
    public record School
    {
        public required string Name { get; init; }
    
        public required string Address { get; init; }
    }
}