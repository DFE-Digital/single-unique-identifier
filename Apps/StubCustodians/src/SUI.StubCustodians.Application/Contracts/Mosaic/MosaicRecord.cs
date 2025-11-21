namespace SUI.StubCustodians.Application.Contracts.Mosaic
{
    public class MosaicRecord : BaseEntity
    {
        public IEnumerable<MosaicReferral>? Referrals { get; init; }
    }
}
