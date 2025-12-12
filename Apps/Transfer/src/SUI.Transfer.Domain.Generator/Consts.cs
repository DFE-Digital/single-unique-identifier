namespace SUI.Transfer.Domain.Generator;

public static class Consts
{
    public const string NewLine =
        @"
";

    public const string TwoNewLines = NewLine + NewLine;

    public const string ExcludeFromCodeCoverageAttributeSource =
        """[ExcludeFromCodeCoverage(Justification = "Generated code; also coverred by behavioural tests in `RecordConsolidationSourceGeneratorTests`")]""";
}
