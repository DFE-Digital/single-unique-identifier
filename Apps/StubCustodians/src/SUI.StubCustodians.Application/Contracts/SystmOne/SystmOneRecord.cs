namespace SUI.StubCustodians.Application.Contracts.SystmOne
{
    public class SystmOneRecord : BaseEntity
    {
        public required string GpName { get; init; }

        public required string GpSurgery { get; init; }

        public required string GpContactNumber { get; init; }

        public IEnumerable<SystmOneMissedAppointment>? MissedAppointmentReasons { get; init; }

        public int MissedAppointments => MissedAppointmentReasons?.Count() ?? 0;
    }
}
