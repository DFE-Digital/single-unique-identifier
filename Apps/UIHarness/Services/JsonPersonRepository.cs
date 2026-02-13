using System.Text.Json;
using UIHarness.Interfaces;
using UIHarness.Models;

namespace UIHarness.Services;

public sealed class JsonPersonRepository : IPersonRepository
{
    private readonly string _jsonPath;
    private readonly JsonSerializerOptions _json;

    public JsonPersonRepository(IWebHostEnvironment env)
    {
        _jsonPath = Path.Combine(env.ContentRootPath, "Data", "people.json");
        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public async Task<IReadOnlyList<PersonRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(_jsonPath);
        var people = await JsonSerializer.DeserializeAsync<List<PersonRecord>>(stream, _json, cancellationToken);
        return people ?? [];
    }
}
