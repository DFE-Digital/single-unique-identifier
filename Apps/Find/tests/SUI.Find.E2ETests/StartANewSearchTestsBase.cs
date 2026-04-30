using System.Diagnostics.CodeAnalysis;

namespace SUI.Find.E2ETests;

public abstract class StartANewSearchTestsBase(
    FunctionTestFixture fixture,
    ITestOutputHelper output
) : SearchTestsBase(fixture, output)
{
    [Theory]
    [MemberData(nameof(HappyPathTestData))]
    [SuppressMessage(
        "Usage",
        "xUnit1045",
        Justification = "The `TestData` is a C# record, and the default string serialization of records provides distinct text for the purposes of test exploration, identification and results."
    )]
    public async Task Should_Return_ExpectedRecords_When_OrchestrationCompletes(TestData testData)
    {
        await RunTest(testData);
    }
}
