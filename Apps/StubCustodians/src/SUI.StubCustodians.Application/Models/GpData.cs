namespace SUI.StubCustodians.Application.Models
{
    public class GpData
    {
        public required string GpName { get; init; }

        public required string GpSurgery { get; init; }

        public required string GpContactNumber { get; init; }

        public IEnumerable<MissedAppointment>? MissedAppointmentReasons { get; init; }

        public int MissedAppointments => MissedAppointmentReasons?.Count() ?? 0;
    }
}
