namespace SUI.Find.Application.Models.Matching;

public class MatchRequest
{
    public KnownData[]  KnownData { get; init; } = Array.Empty<KnownData>();
    
    public PersonSpecification DemographicRecord { get; init; } = new PersonSpecification();
}