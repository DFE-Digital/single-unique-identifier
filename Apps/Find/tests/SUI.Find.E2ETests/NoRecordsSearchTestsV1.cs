namespace SUI.Find.E2ETests;

public class NoRecordsSearchTestsV1(FunctionTestFixture fixture, ITestOutputHelper output)
    : NoRecordsSearchTestsBase(fixture, output)
{
    protected override bool UsePolling => false;
}
