using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SUI.StubCustodians.Application.Extensions;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Infrastructure.Extensions;

namespace SUI.StubCustodians.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class FileDataProvider(
    IRandomDelayService throttleService,
    IConfiguration configuration
) : IDataProvider
{
    private readonly Dictionary<string, CustodianConfig> _cache = new(
        StringComparer.OrdinalIgnoreCase
    );

    public async Task<IReadOnlyList<CustodianRecord>> GetRecordsAsync(
        string orgId,
        string personId,
        CancellationToken cancellationToken
    )
    {
        await throttleService.DelayAsync(cancellationToken);

        var cfg = await LoadAsync(orgId, cancellationToken);
        var useEncryptedId = configuration.UseEncryptedId();

        return cfg
            .Records.Where(r =>
                string.Equals(
                    useEncryptedId ? r.EncryptedPersonId : r.PersonId,
                    personId,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .ToList();
    }

    public async Task<IReadOnlyList<CustodianRecord>> GetRecordsAsync(
        string orgId,
        string recordType,
        string personId,
        CancellationToken cancellationToken
    )
    {
        await throttleService.DelayAsync(cancellationToken);

        var cfg = await LoadAsync(orgId, cancellationToken);
        var useEncryptedId = configuration.UseEncryptedId();

        return cfg
            .Records.Where(r =>
                string.Equals(
                    useEncryptedId ? r.EncryptedPersonId : r.PersonId,
                    personId,
                    StringComparison.OrdinalIgnoreCase
                ) && string.Equals(r.RecordType, recordType, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();
    }

    public async Task<CustodianRecord?> GetRecordByIdAsync(
        string orgId,
        string recordId,
        CancellationToken cancellationToken
    )
    {
        await throttleService.DelayAsync(cancellationToken);

        var cfg = await LoadAsync(orgId, cancellationToken);

        return cfg.Records.FirstOrDefault(r =>
            string.Equals(r.RecordId, recordId, StringComparison.OrdinalIgnoreCase)
        );
    }

    private async Task<CustodianConfig> LoadAsync(string orgId, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(orgId, out var cached))
        {
            return cached;
        }

        var fileName = $"{orgId.ToLowerInvariant()}.custodian.json";
        var baseDir = AppContext.BaseDirectory;
        var path = Path.Combine(baseDir, "Data", fileName);

        await using var stream = File.OpenRead(path);

        var cfg = await JsonSerializer.DeserializeAsync<CustodianConfig>(
            stream,
            JsonSerializerOptions.Web,
            cancellationToken
        );

        var materialised =
            cfg
            ?? throw new InvalidOperationException(
                $"Custodian config '{fileName}' could not be deserialised."
            );

        _cache[orgId] = materialised;
        return materialised;
    }

    private sealed class CustodianConfig
    {
        public string OrgId { get; set; } = null!;
        public string OrgName { get; set; } = null!;
        public string OrgType { get; set; } = null!;
        public List<CustodianRecord> Records { get; set; } = [];
    }
}
