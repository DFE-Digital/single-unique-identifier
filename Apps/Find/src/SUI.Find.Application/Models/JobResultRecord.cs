using SUI.Find.Application.Constants;

namespace SUI.Find.Application.Models;

public record JobResultRecord
{
    public required string RecordType { get; init; }

    public required string RecordUrl { get; init; }

    public string SystemId
    {
        get;
        init =>
            field = string.IsNullOrWhiteSpace(value)
                ? ApplicationConstants.SystemIds.Default
                : value;
    } = ApplicationConstants.SystemIds.Default;

    public string? RecordId { get; init; }
}
