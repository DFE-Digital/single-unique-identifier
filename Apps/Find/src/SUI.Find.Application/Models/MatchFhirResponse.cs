namespace SUI.Find.Application.Models;

public abstract record MatchFhirResponse
{
    public sealed record Match(string NhsNumber) : MatchFhirResponse;

    public sealed record NoMatch : MatchFhirResponse;

    public sealed record Error(string ErrorMessage) : MatchFhirResponse;
}
