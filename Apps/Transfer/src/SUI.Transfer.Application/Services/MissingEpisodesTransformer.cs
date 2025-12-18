using SUI.Custodians.API.Client;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class MissingEpisodesTransformer(TimeProvider timeProvider) : IMissingEpisodesTransformer
{
    public CrimeMissingEpisodesSummaries? ApplyTransformation(ConsolidatedData consolidatedData)
    {
        if (
            consolidatedData.CrimeDataRecord?.MissingEpisodes?.Value is null
            || consolidatedData.CrimeDataRecord.MissingEpisodes.Value.Count.Equals(0)
        )
            return null;

        return new CrimeMissingEpisodesSummaries
        {
            Last6Months = GetMissingEpisodesFromLast6Months(consolidatedData),
        };
    }

    private List<CrimeMissingEpisodeV1> GetMissingEpisodesFromLast6Months(
        ConsolidatedData consolidatedData
    )
    {
        var sixMonthsAgo = timeProvider.GetUtcNow().AddMonths(-6);

        return (consolidatedData.CrimeDataRecord?.MissingEpisodes?.Value ?? [])
            .Where(x => x.Date.HasValue && x.Date.Value >= sixMonthsAgo)
            .ToList();
    }
}
