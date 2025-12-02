namespace SUI.Custodians.Domain.Models;

public record AddressV1
{
    public string? Line1 { get; init; }

    public string? Line2 { get; init; }

    public string? TownOrCity { get; init; }

    public string? County { get; init; }

    public string? Postcode { get; init; }
}
