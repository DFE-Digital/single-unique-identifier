using SUI.Custodians.API.Client;

namespace SUI.Transfer.Domain;

public record CrimeMissingEpisodesSummaries
{
    public required IReadOnlyCollection<CrimeMissingEpisodeV1>? Last6Months { get; init; }
}
