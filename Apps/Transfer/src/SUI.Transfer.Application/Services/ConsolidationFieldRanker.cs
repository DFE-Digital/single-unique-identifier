namespace SUI.Transfer.Application.Services;

public class ConsolidationFieldRanker : IConsolidationFieldRanker
{
    /// <summary>
    /// Returns a ranking for the specified combination of Provider ID, record type and field name.
    /// This ranking is used to choose a value for a field which is found in multiple providers.
    /// A lower value indicates a higher precedence, i.e. during consolidation lower rank values are chosen over higher values.
    /// e.g. a value of 1 indicates a higher precedence than a value of 2.
    /// </summary>
    public int RankField(
        string providerSystemId,
        string recordName,
        string propertyName,
        string recordAndPropertyName
    )
    {
        return 0;
    }
}
