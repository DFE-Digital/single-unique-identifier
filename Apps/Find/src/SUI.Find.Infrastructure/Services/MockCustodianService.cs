using System.IO.Abstractions;
using System.Text.Json;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;

namespace SUI.Find.Infrastructure.Services;

public class MockCustodianService(IFileSystem fileSystem) : ICustodianService
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IReadOnlyList<ProviderDefinition>> GetCustodiansAsync()
    {
        var baseDir = AppContext.BaseDirectory;
        var filePath = Path.Combine(baseDir, "Data", "org-directory.json");

        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"Auth store file not found at: {filePath}");
        }

        var json = await fileSystem.File.ReadAllTextAsync(filePath);
        var doc =
            JsonSerializer.Deserialize<OrgDirectory>(json, _jsonSerializerOptions)
            ?? throw new InvalidOperationException("Failed to deserialize org-directory.json");

        var providers = new List<ProviderDefinition>();

        foreach (var org in doc.Organisations)
        {
            foreach (var record in org.Records)
            {
                var conn = record.Connection;
                providers.Add(
                    new ProviderDefinition
                    {
                        OrgId = org.OrgId,
                        OrgName = org.OrgName,
                        OrgType = org.OrgType,
                        ProviderSystem = org.OrgId,
                        ProviderName = org.OrgName,
                        RecordType = record.RecordType,
                        Connection = new ConnectionDefinition
                        {
                            Method = conn.Method,
                            Url = conn.Url,
                            PersonIdPosition = conn.PersonIdPosition,
                            Auth = new AuthDefinition
                            {
                                Type = conn.Auth.Type,
                                TokenUrl = conn.Auth.TokenUrl,
                                Scopes = conn.Auth.Scopes?.ToArray() ?? [],
                                ClientId = conn.Auth.ClientId ?? string.Empty,
                                ClientSecret = conn.Auth.ClientSecret,
                            },
                            BodyTemplateJson = conn.BodyTemplate.ToString(),
                        },
                        Encryption =
                            org.Encryption == null
                                ? null
                                : new EncryptionDefinition
                                {
                                    Algorithm = org.Encryption.Algorithm,
                                    KeyId = org.Encryption.KeyId,
                                    Key = org.Encryption.Key,
                                },
                    }
                );
            }
        }

        return providers;
    }
}

public class OrgDirectory
{
    public List<Organisation> Organisations { get; set; }
}

public class Organisation
{
    public string OrgId { get; set; }
    public string OrgName { get; set; }
    public string OrgType { get; set; }
    public List<Record> Records { get; set; }
    public DsaPolicy DsaPolicy { get; set; }
    public Encryption Encryption { get; set; }
}

public class Record
{
    public string RecordType { get; set; }
    public Connection Connection { get; set; }
}

public class Connection
{
    public string Method { get; set; }
    public string Url { get; set; }
    public string PersonIdPosition { get; set; }
    public Dictionary<string, object> BodyTemplate { get; set; } = [];
    public Auth Auth { get; set; }
}

public class Auth
{
    public string Type { get; set; }
    public string TokenUrl { get; set; }
    public List<string> Scopes { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
}

public class DsaPolicy
{
    public string Version { get; set; }
    public List<DsaRule> Defaults { get; set; }
    public List<DsaException> Exceptions { get; set; }
}

public class DsaRule
{
    public string Effect { get; set; }
    public List<string> Modes { get; set; }
    public List<string> DataTypes { get; set; }
    public List<string> DestOrgTypes { get; set; }
    public List<string> Purposes { get; set; }
    public string ValidFrom { get; set; }
}

public class DsaException
{
    public string Effect { get; set; }
    public List<string> Modes { get; set; }
    public List<string> DataTypes { get; set; }
    public List<string> DestOrgTypes { get; set; }
    public List<string> DestOrgIds { get; set; }
    public List<string> Purposes { get; set; }
    public string ValidFrom { get; set; }
    public string ValidUntil { get; set; }
    public string Reason { get; set; }
}

public class Encryption
{
    public string Algorithm { get; set; }
    public string KeyId { get; set; }
    public string Key { get; set; }
}
