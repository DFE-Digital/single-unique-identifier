namespace SUI.Transfer.Domain.Generator.Tests.ExampleModels;

public record ExampleRecord1
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTimeOffset? DateOfBirth { get; set; }
}
