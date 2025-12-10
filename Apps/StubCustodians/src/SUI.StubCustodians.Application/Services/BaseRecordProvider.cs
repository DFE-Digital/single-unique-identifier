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
        _basePath = basePath ?? Path.Combine(Directory.GetCurrentDirectory(), "SampleData");
    }

    public virtual RecordEnvelope<T>? GetRecordForSui(string sui, string providerSystemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sui);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSystemId);

        string filePath = Path.Combine(_basePath, providerSystemId, GetFileName(sui));

        if (!File.Exists(filePath))
        {
            return null;
        }

        var record = JsonSerializer.Deserialize<T>(File.ReadAllText(filePath), _jsonOptions);

        if (record == null)
        {
            return null;
        }

        return new RecordEnvelope<T> { SchemaUri = new Uri(GetSchemaUri()), Payload = record };
    }

    private string GetSchemaUri()
    {
        return $"https://sui.gov.uk/schemas/{typeof(T).Name}.json";
    }

    private string GetFileName(string sui)
    {
        return $"{sui}_{typeof(T).Name}.json";
    }
}
