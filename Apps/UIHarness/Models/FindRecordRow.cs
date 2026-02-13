namespace UIHarness.Models;

public sealed class FindRecordRow
{
    public Guid SearchId { get; set; }
    public string CustodianName { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public string RecordUrl { get; set; } = string.Empty;
}