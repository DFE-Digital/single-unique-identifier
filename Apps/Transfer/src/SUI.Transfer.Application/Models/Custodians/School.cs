namespace SUI.Transfer.Application.Models.Custodians;

public record School
{
    public required string Name { get; init; }

    public required string Address { get; init; }
}
