namespace SUI.Find.Application.Models.Matching;

public class MatchRequest
{
    public Metadata[]? Metadata { get; init; }

    public required PersonSpecification PersonSpecification { get; init; }
}
