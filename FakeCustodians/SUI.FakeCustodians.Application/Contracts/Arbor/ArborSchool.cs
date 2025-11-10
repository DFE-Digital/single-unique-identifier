namespace SUI.FakeCustodians.Application.Contracts.Arbor
{
    public record ArborSchool
    {
        public required string Name { get; init; }
        
        public required string Address { get; init; }
    }
}