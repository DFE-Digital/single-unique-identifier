namespace SUI.Transfer.Application.Services;

public record Custodian
{
    public required RecordType RecordType { get; init; }

    public required string RecordLocation { get; init; }
}
