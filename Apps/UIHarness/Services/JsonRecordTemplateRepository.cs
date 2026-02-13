using System.Text;
using System.Text.Json;
using UIHarness.Interfaces;
using UIHarness.Models;

namespace UIHarness.Services;

public sealed class JsonRecordTemplateRepository : IRecordTemplateRepository
{
    private readonly string _jsonPath;
    private readonly JsonSerializerOptions _json;

    public JsonRecordTemplateRepository(IWebHostEnvironment env)
    {
        _jsonPath = Path.Combine(env.ContentRootPath, "Data", "record-templates.json");
        _json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public async Task<RecordTemplate?> GetByRecordTypeAsync(string recordType, CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(_jsonPath, Encoding.UTF8, cancellationToken);
        var file = JsonSerializer.Deserialize<RecordTemplatesFile>(text, _json);

        return file?.Templates?
            .FirstOrDefault(t => string.Equals(t.RecordType, recordType, StringComparison.Ordinal));
    }
}