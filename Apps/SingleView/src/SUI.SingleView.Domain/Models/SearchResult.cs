namespace SUI.SingleView.Domain.Models;

public record SearchResult
{
    public required NhsNumber NhsNumber { get; init; }
    public required string Name { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public required Address Address { get; init; }
}
