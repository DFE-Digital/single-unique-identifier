namespace UIHarness.Models;

public sealed class CustodianSearchCompleted
{
    public Guid SearchId { get; set; }
    public string CustodianId { get; set; } = string.Empty;
    public bool HasMatch { get; set; }
}
