namespace UIHarness.Models;

public sealed class FindRecordsStarted
{
    public Guid SearchId { get; set; }
    public string NhsNumber { get; set; } = string.Empty;
    public int TotalCustodians { get; set; }
}
