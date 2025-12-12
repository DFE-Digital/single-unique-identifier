namespace SUI.Transfer.Domain.Generator.Tests.MoreExampleModels;

public record ExampleRecord3
{
    public ICollection<string>? ExampleStringList { get; set; }

    public int[]? ExampleNumberList { get; set; }

    public ExampleDto[]? ExampleDtoList { get; set; }
}

public record ExampleDto(string Name, bool Flag);
