namespace UIHarness.Models;

public sealed class FindRecordsRequest
{
    public Guid PersonId { get; set; }
    public string NhsNumber { get; set; } = string.Empty;
}