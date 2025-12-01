using System.IO.Abstractions;
using System.Text.Json;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable UnusedMember.Local

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
            JsonSerializer.Deserialize<MockOrgDirectory>(json, _jsonSerializerOptions)
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
                                Scopes = conn.Auth.Scopes.ToArray() ?? [],
                                ClientId = conn.Auth.ClientId,
                                ClientSecret = conn.Auth.ClientSecret,
                            },
                            BodyTemplateJson = conn.BodyTemplate?.ToString(),
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

    private class MockOrgDirectory
    {
        public List<MockOrganisation> Organisations { get; set; } = null!;
    }

    private class MockOrganisation
    {
        public string OrgId { get; set; } = null!;
        public string OrgName { get; set; } = null!;
        public string OrgType { get; set; } = null!;
        public List<MockRecord> Records { get; set; } = null!;
        public MockDsaPolicy DsaPolicy { get; set; } = null!;
        public MockEncryption Encryption { get; set; } = null!;
    }

    private class MockRecord
    {
        public string RecordType { get; set; } = null!;
        public MockConnection Connection { get; set; } = null!;
    }

    private class MockConnection
    {
        public string Method { get; set; } = null!;
        public string Url { get; set; } = null!;
        public string PersonIdPosition { get; set; } = null!;
        public Dictionary<string, object>? BodyTemplate { get; set; } = null;
        public MockAuth Auth { get; set; } = null!;
    }

    private class MockAuth
    {
        public string Type { get; set; } = null!;
        public string TokenUrl { get; set; } = null!;
        public List<string> Scopes { get; set; } = null!;
        public string ClientId { get; set; } = null!;
        public string ClientSecret { get; set; } = null!;
    }

    private class MockDsaPolicy
    {
        public string Version { get; set; } = null!;
        public List<MockDsaRule> Defaults { get; set; } = null!;
        public List<MockDsaException> Exceptions { get; set; } = null!;
    }

    private class MockDsaRule
    {
        public string Effect { get; set; } = null!;
        public List<string> Modes { get; set; } = null!;
        public List<string> DataTypes { get; set; } = null!;
        public List<string> DestOrgTypes { get; set; } = null!;
        public List<string> Purposes { get; set; } = null!;
        public string ValidFrom { get; set; } = null!;
    }

    private class MockDsaException
    {
        public string Effect { get; set; } = null!;
        public List<string> Modes { get; set; } = null!;
        public List<string> DataTypes { get; set; } = null!;
        public List<string> DestOrgTypes { get; set; } = null!;
        public List<string> DestOrgIds { get; set; } = null!;
        public List<string> Purposes { get; set; } = null!;
        public string ValidFrom { get; set; } = null!;
        public string ValidUntil { get; set; } = null!;
        public string Reason { get; set; } = null!;
    }

    private class MockEncryption
    {
        public string Algorithm { get; set; } = null!;
        public string KeyId { get; set; } = null!;
        public string Key { get; set; } = null!;
    }
}
