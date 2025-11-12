namespace SUI.FakeCustodians.Application.Contracts.SystmOne;

public record MissedAppointmentReason
{
    public DateTime Date { get; init; }

    public string? Reason { get; init; }

    public required string Location { get; init; }
}
