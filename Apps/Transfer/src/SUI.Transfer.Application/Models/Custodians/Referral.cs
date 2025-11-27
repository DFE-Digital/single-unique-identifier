namespace SUI.Transfer.Application.Models.Custodians;

public record Referral
{
    public required string Id { get; init; }

    public required DateTime Date { get; init; }

    public required string Reason { get; init; }
}
