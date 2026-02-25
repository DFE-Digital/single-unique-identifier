namespace SUI.UIHarness.Web.Models.Find;

public class FindMatchRequest
{
    public FindMatchMetadata[]? Metadata { get; init; }

    public required FindMatchPerson PersonSpecification { get; init; }
}
