using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IMissingEpisodesTransformer
{
    CrimeMissingEpisodesSummaries? ApplyTransformation(ConsolidatedData consolidatedData);
}
