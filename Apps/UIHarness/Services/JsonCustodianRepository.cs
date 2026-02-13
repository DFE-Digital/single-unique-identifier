using System.Text;
using System.Text.Json;
using UIHarness.Interfaces;
using UIHarness.Models;

namespace UIHarness.Services;

public sealed class JsonCustodianRepository : ICustodianRepository
{
    private readonly string _jsonPath;
    private readonly JsonSerializerOptions _json;

    public JsonCustodianRepository(IWebHostEnvironment env)
    {
        _jsonPath = Path.Combine(env.ContentRootPath, "Data", "custodians.json");
        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public async Task<IReadOnlyList<Custodian>> GetAllAsync(CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(_jsonPath, Encoding.UTF8, cancellationToken);
        var custodians = JsonSerializer.Deserialize<List<Custodian>>(text, _json);
        return custodians ?? [];
    }

    public async Task<Custodian?> GetByIdAsync(string custodianId, CancellationToken cancellationToken)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.FirstOrDefault(c => string.Equals(c.CustodianId, custodianId, StringComparison.Ordinal));
    }
}