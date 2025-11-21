namespace SUI.FakeCustodians.Application.Contracts.Mosaic
{
    public record MosaicReferral
    {
        public required string Id { get; init; }

        public required DateTime Date { get; init; }

        public required string Reason { get; init; }
    }
}
