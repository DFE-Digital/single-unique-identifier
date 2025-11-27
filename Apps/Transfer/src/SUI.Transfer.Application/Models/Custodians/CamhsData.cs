namespace SUI.Transfer.Application.Models.Custodians;

public record CamhsData : ICustodianRecord
{
    public IEnumerable<Referral>? Referrals { get; init; }
}
