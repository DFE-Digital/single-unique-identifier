namespace SUI.FakeCustodians.Application.Models
{
    public record EventResponse
    {
        public required string Sui { get; init; } 
    
        public ConsolidatedEventData? Data { get; init; }
    }
}