using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.Application.Models;

public abstract record MatchPersonResponse
{
    public sealed record Match(EncryptedPersonId PersonId) : MatchPersonResponse;

    public sealed record NoMatch() : MatchPersonResponse;

    public sealed record Error(string ErrorMessage) : MatchPersonResponse;
}
