namespace SUI.Transfer.Application.Services;

public interface IConsolidationFieldRanker
{
    int RankField(
        string providerSystemId,
        string recordName,
        string propertyName,
        string recordAndPropertyName
    );
}
