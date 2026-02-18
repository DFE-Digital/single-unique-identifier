namespace SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;

public class IdRegisterEntry
{
    public required string Sui { get; init; }
    public required string CustodianId { get; init; }
    public required string SystemId { get; init; }
    public required string RecordType { get; init; }
    public string? CustodianSubjectId { get; init; }
    public DateTimeOffset? FirstSeenUtc { get; init; }
    public DateTimeOffset? LastSeenUtc { get; init; }
    public required Provenance Provenance { get; init; }
    public DateTimeOffset? LastIdDeliveredAtUtc { get; init; }
}

public enum Provenance
{
    Unknown,
    IssuedByService,
    AlreadyHeldByCustodian,
    DiscoveredViaFanout,
}
