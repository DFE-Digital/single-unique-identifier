namespace SUI.Transfer.Application.Models.Custodians;

public record PersonalData : ICustodianRecord
{
    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public DateTime DateOfBirth { get; init; }

    public string? NhsNumber { get; init; }
}
