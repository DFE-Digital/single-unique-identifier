using Interfaces;
using Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services;

public sealed class CustodianRegistryService : ICustodianRegistry
{
    private readonly IReadOnlyList<ProviderDefinition> _providers;

    public CustodianRegistryService()
    {
        var baseDir = AppContext.BaseDirectory;
        var filePath = Path.Combine(baseDir, "Data", "org-directory.json");
        _providers = LoadFromDirectoryFile(filePath);
    }

    public IReadOnlyList<ProviderDefinition> GetCustodians() => _providers;

    private static IReadOnlyList<ProviderDefinition> LoadFromDirectoryFile(string path)
    {
        if (!File.Exists(path))
        {
            return Array.Empty<ProviderDefinition>();
        }

        using var stream = File.OpenRead(path);

        var directory = JsonSerializer.Deserialize<OrgDirectory>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (directory is null || directory.Organisations.Count == 0)
        {
            return Array.Empty<ProviderDefinition>();
        }

        var providers = new List<ProviderDefinition>();

        foreach (var org in directory.Organisations)
        {
            foreach (var rec in org.Records)
            {
                var connection = rec.Connection ?? (rec.SearchEndpoint is null
                    ? null
                    : new ConnectionDefinition
                    {
                        Method = "GET",
                        Url = rec.SearchEndpoint,
                        PersonIdPosition = "path"
                    });

                if (connection is null)
                {
                    continue;
                }

                providers.Add(new ProviderDefinition
                {
                    OrgId = org.OrgId,
                    OrgName = org.OrgName,
                    OrgType = org.OrgType,
                    ProviderSystem = org.OrgId,
                    ProviderName = org.OrgName,
                    RecordType = rec.RecordType,
                    Connection = connection,
                    Encryption = org.Encryption
                });
            }
        }

        return providers;
    }

    private sealed class OrgDirectory
    {
        [JsonPropertyName("organisations")]
        public List<OrganisationEntry> Organisations { get; init; } = new();
    }

    private sealed class OrganisationEntry
    {
        [JsonPropertyName("orgId")]
        public string OrgId { get; init; } = string.Empty;

        [JsonPropertyName("orgName")]
        public string OrgName { get; init; } = string.Empty;

        [JsonPropertyName("orgType")]
        public string OrgType { get; init; } = string.Empty;

        [JsonPropertyName("records")]
        public List<RecordEntry> Records { get; init; } = new();

        [JsonPropertyName("encryption")]
        public EncryptionDefinition? Encryption { get; init; }
    }

    private sealed class RecordEntry
    {
        [JsonPropertyName("recordType")]
        public string RecordType { get; init; } = string.Empty;

        [JsonPropertyName("connection")]
        public ConnectionDefinition? Connection { get; init; }

        [JsonPropertyName("searchEndpoint")]
        public string? SearchEndpoint { get; init; }
    }
}
