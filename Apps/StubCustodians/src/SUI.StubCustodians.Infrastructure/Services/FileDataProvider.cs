using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class FileDataProvider(IRandomDelayService throttleService) : IDataProvider
{
    private readonly Dictionary<string, CustodianConfig> _cache = new(
        StringComparer.OrdinalIgnoreCase
    );

    // Build an explicit whitelist mapping of OrgId -> Absolute File Path.
    private readonly Lazy<Dictionary<string, string>> _fileMap = new(() =>
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var baseDir = AppContext.BaseDirectory;
        var dataDir = Path.Combine(baseDir, "Data");

        if (Directory.Exists(dataDir))
        {
            var files = Directory.GetFiles(dataDir, "*.custodian.json");
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                // Extract the orgId from the file name to use as our dictionary key
                var key = fileName.Replace(
                    ".custodian.json",
                    "",
                    StringComparison.OrdinalIgnoreCase
                );
                map[key] = file;
            }
        }

        return map;
    });

    public async Task<IReadOnlyList<CustodianRecord>> GetRecordsAsync(
        string orgId,
        string personId,
        CancellationToken cancellationToken
    )
    {
        await throttleService.DelayAsync(cancellationToken);

        var cfg = await LoadAsync(orgId, cancellationToken);

        return cfg
            .Records.Where(r =>
                string.Equals(r.PersonId, personId, StringComparison.OrdinalIgnoreCase)
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

        return cfg
            .Records.Where(r =>
                string.Equals(r.PersonId, personId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(r.RecordType, recordType, StringComparison.OrdinalIgnoreCase)
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

        // Validate the user-provided orgId strictly against the pre-loaded dictionary
        if (!_fileMap.Value.TryGetValue(orgId, out var path))
        {
            throw new InvalidOperationException(
                $"No custodian configuration found for organization '{orgId}'."
            );
        }

        await using var stream = File.OpenRead(path);

        var cfg = await JsonSerializer.DeserializeAsync<CustodianConfig>(
            stream,
            JsonSerializerOptions.Web,
            cancellationToken
        );

        var materialised =
            cfg
            ?? throw new InvalidOperationException(
                $"Custodian config '{Path.GetFileName(path)}' could not be deserialised."
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
