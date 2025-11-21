namespace SUI.FakeCustodians.Application.Contracts.SystmOne
{
    public class SystmOneMissedAppointment
    {
        public DateTime Date { get; init; }

        public string? Reason { get; init; }

        public required string Location { get; init; }
    }
}
