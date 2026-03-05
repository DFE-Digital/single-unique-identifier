namespace SUI.UIHarness.Web.Models.Records;

public record Address
{
    /// <summary>
    /// Line 1 of the address
    /// </summary>
    /// <example>123 Kew Gardens</example>
    public string? Line1 { get; init; }

    /// <summary>
    /// Line 2 of the address
    /// </summary>
    /// <example>Bakers Lane</example>
    public string? Line2 { get; init; }

    /// <summary>
    /// Town or city
    /// </summary>
    /// <example>Bath</example>
    public string? TownOrCity { get; init; }

    /// <summary>
    /// County
    /// </summary>
    /// <example>Somerset</example>
    public string? County { get; init; }

    /// <summary>
    /// Postcode
    /// </summary>
    /// <example>BA1 2AB</example>
    public string? Postcode { get; init; }
}
