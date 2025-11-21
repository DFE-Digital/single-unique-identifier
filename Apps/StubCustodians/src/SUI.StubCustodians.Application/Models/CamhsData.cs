namespace SUI.StubCustodians.Application.Models
{
    public record CamhsData
    {
        public IEnumerable<Referral>? Referrals { get; init; }
    }
}
