namespace UIHarness.Models;

public sealed class RecordDataSection
{
    public string Title { get; set; } = string.Empty;
    public List<RecordField> Fields { get; set; } = [];
}
