namespace UIHarness.Models;

public sealed class FetchRecordResponse
{
    public string CustodianId { get; set; } = string.Empty;
    public string CustodianName { get; set; } = string.Empty;

    public string NhsNumber { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;

    public List<ContactRecord> Contacts { get; set; } = [];

    public List<RecordDataSection> DataSections { get; set; } = [];

    public ExternalSystemLink? ExternalSystem { get; set; }

    public string Summary { get; set; } = string.Empty;
}
