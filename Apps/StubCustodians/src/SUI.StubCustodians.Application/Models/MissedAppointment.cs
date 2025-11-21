namespace SUI.StubCustodians.Application.Models;

public class MissedAppointment
{
    public DateTime Date { get; init; }

    public string? Reason { get; init; }

    public required string Location { get; init; }
}
