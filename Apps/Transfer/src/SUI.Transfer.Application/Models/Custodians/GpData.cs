namespace SUI.Transfer.Application.Models.Custodians;

public class GpData : ICustodianRecord
{
    public required string GpName { get; init; }

    public required string GpSurgery { get; init; }

    public required string GpContactNumber { get; init; }

    public IEnumerable<MissedAppointment>? MissedAppointmentReasons { get; init; }

    public int MissedAppointments => MissedAppointmentReasons?.Count() ?? 0;
}
