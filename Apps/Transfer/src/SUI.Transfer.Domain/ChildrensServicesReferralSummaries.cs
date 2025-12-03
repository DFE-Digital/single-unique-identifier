namespace SUI.Transfer.Domain;

public record ChildrensServicesReferralSummaries
{
    public required ChildrensServicesReferralSummary[]? Past6Months { get; init; }

    public required ChildrensServicesReferralSummary[]? Past12Months { get; init; }

    public required ChildrensServicesReferralSummary[]? Past5Years { get; init; }
}
