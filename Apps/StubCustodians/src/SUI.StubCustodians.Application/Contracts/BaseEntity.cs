namespace SUI.FakeCustodians.Application.Contracts;

public abstract class BaseEntity
{
    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public DateTime DateOfBirth { get; init; }

    public string? NhsNumber { get; init; }
}
