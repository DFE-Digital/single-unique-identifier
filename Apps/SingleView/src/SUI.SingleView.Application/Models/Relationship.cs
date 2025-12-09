namespace SUI.SingleView.Application.Models;

public class Relationship
{
    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string DateOfBirth { get; init; } = string.Empty;

    public string Risk { get; init; } = string.Empty;

    public List<string> ServicesKnownTo { get; init; } = [];
}
