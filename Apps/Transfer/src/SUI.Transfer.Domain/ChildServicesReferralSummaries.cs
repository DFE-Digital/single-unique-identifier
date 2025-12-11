namespace SUI.Transfer.Domain;

public record ChildServicesReferralSummaries
{
    public required IReadOnlyCollection<ChildServicesReferralSummary>? Past6Months { get; init; }

    public required IReadOnlyCollection<ChildServicesReferralSummary>? Past12Months { get; init; }

    public required IReadOnlyCollection<ChildServicesReferralSummary>? Past5Years { get; init; }
}
