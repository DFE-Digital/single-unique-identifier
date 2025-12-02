namespace SUI.Transfer.Domain;

public record ChildrensSocialCareReferralSummaries
{
    public required ChildrensSocialCareReferralSummary[]? Past6Months { get; init; }

    public required ChildrensSocialCareReferralSummary[]? Past12Months { get; init; }

    public required ChildrensSocialCareReferralSummary[]? Past5Years { get; init; }
}
