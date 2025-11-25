using System.Text.Json;
using Interfaces;

namespace Functions;

public sealed class FileDataProvider(IRandomDelayService thottler) : IDataProvider
{
    private readonly Dictionary<string, CustodianConfig> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly IRandomDelayService _thottler = thottler;

    public async Task<IReadOnlyList<CustodianRecord>> GetRecordsAsync(string orgId, string personId, CancellationToken cancellationToken)
    {
        await _thottler.DelayAsync(cancellationToken);

        var cfg = await LoadAsync(orgId, cancellationToken);

        return cfg.Records
            .Where(r => string.Equals(r.PersonId, personId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IReadOnlyList<CustodianRecord>> GetRecordsAsync(string orgId, string recordType, string personId, CancellationToken cancellationToken)
    {
        await _thottler.DelayAsync(cancellationToken);

        var cfg = await LoadAsync(orgId, cancellationToken);

        return cfg.Records
            .Where(r =>
                string.Equals(r.PersonId, personId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.RecordType, recordType, StringComparison.OrdinalIgnoreCase))
            .ToList();
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
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        var materialised = cfg ?? throw new InvalidOperationException($"Custodian config '{fileName}' could not be deserialised.");

        _cache[orgId] = materialised;
        return materialised;
    }

    private sealed class CustodianConfig
    {
        public string OrgId { get; set; } = default!;
        public string OrgName { get; set; } = default!;
        public string OrgType { get; set; } = default!;
        public List<CustodianRecord> Records { get; set; } = new();
    }
}
