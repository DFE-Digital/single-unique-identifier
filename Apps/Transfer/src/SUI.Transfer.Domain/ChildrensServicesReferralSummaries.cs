namespace SUI.Transfer.Domain;

public record ChildrensServicesReferralSummaries
{
    public required IReadOnlyCollection<ChildrensServicesReferralSummary>? Past6Months { get; init; }

    public required IReadOnlyCollection<ChildrensServicesReferralSummary>? Past12Months { get; init; }

    public required IReadOnlyCollection<ChildrensServicesReferralSummary>? Past5Years { get; init; }
}
