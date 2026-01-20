using System.Security;
using System.Text.Json;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Services;

public abstract class BaseRecordProvider<T> : IRecordProvider<T>
    where T : class
{
    private readonly string _basePath;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    protected BaseRecordProvider(string? basePath = null)
    {
        _basePath = basePath ?? Path.Combine(Directory.GetCurrentDirectory(), "Data");
    }

    public virtual RecordEnvelope<T>? GetRecordForSui(string sui, string providerSystemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sui);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSystemId);

        string filePath = Path.Combine(_basePath, providerSystemId, GetFileName(providerSystemId));

        // Prevent path traversal
        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException("Invalid file path.");
        }

        if (!File.Exists(fullPath))
        {
            return null;
        }

        var json = File.ReadAllText(fullPath);
        var record = JsonSerializer.Deserialize<T>(json, _jsonOptions);

        if (record == null)
        {
            return null;
        }

        return new RecordEnvelope<T> { SchemaUri = new Uri(GetSchemaUri()), Payload = record };
    }

    private static string GetSchemaUri()
    {
        return $"https://schemas.example.gov.uk/sui/{typeof(T).Name}.json";
    }

    private static string GetFileName(string providerSystemId, string version = "V1")
    {
        return $"{providerSystemId}_custodian.json";
    }
}
