using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Services;

public class ManifestService(IDataProvider store) : IManifestService
{
    public async Task<IList<SearchResultItem>> GetManifestForOrganisation(
        string orgId,
        string personId,
        string baseUrl,
        string? recordType,
        CancellationToken cancellationToken
    )
    {
        var records = string.IsNullOrWhiteSpace(recordType)
            ? await store.GetRecordsAsync(orgId, personId, cancellationToken)
            : await store.GetRecordsAsync(orgId, recordType, personId, cancellationToken);

        var items = records
            .Select(r => new SearchResultItem(
                r.RecordType,
                BuildRecordUrl(baseUrl, orgId, r.RecordType, r.RecordId),
                r.RecordId,
                orgId
            ))
            .ToList();

        return items;
    }

    private static string BuildRecordUrl(
        string baseUrl,
        string orgId,
        string recordType,
        string recordId
    )
    {
        return orgId.ToUpperInvariant() switch
        {
            "LOCAL-AUTHORITY-01" or "HEALTH-01" =>
                $"{baseUrl}/api/v1/fetch/{Uri.EscapeDataString(orgId.ToLowerInvariant())}/{Uri.EscapeDataString(recordId)}?recordType={Uri.EscapeDataString(recordType)}",
            _ =>
                $"{baseUrl}/api/v1/fetch/{Uri.EscapeDataString(orgId.ToLowerInvariant())}/{Uri.EscapeDataString(recordType)}/{Uri.EscapeDataString(recordId)}",
        };
    }
}
