namespace SUI.SingleView.Application.Models;

public class Relationship
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string DateOfBirth { get; set; } = string.Empty;

    public string Risk { get; set; } = string.Empty;

    public List<string> ServicesKnownTo { get; set; } = [];
}
