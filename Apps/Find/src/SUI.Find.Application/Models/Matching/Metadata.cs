using SUI.Find.Application.Constants;

namespace SUI.Find.Application.Models.Matching;

public class Metadata
{
    public required string RecordType { get; set; }

    public string SystemId
    {
        get;
        init =>
            field = string.IsNullOrWhiteSpace(value)
                ? ApplicationConstants.SystemIds.Default
                : value;
    } = ApplicationConstants.SystemIds.Default;

    public string? RecordId { get; set; }
}
