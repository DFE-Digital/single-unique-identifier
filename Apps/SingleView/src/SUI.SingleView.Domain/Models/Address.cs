namespace SUI.SingleView.Domain.Models;

public record Address
{
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? Postcode { get; init; }
    public string? Town { get; init; }
    public string? County { get; init; }
    public string? Country { get; init; }

    public string ToSingleLine() => string.Join(", ", _getParts());

    public string ToMultiLine() => string.Join("\n", _getParts());

    private List<string> _getParts()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(AddressLine1))
            parts.Add(AddressLine1);

        if (!string.IsNullOrWhiteSpace(AddressLine2))
            parts.Add(AddressLine2);

        if (!string.IsNullOrWhiteSpace(Town))
            parts.Add(Town);

        if (!string.IsNullOrWhiteSpace(County))
            parts.Add(County);

        if (!string.IsNullOrWhiteSpace(Postcode))
            parts.Add(Postcode);

        return parts;
    }
};
