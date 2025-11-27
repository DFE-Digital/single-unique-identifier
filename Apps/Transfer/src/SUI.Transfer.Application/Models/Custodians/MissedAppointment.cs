namespace SUI.Transfer.Application.Models.Custodians;

public class MissedAppointment : ICustodianRecord
{
    public DateTime Date { get; init; }

    public string? Reason { get; init; }

    public required string Location { get; init; }
}
