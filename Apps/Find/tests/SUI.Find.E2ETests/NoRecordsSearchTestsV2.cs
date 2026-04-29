namespace SUI.Find.E2ETests;

public class NoRecordsSearchTestsV2(FunctionTestFixture fixture, ITestOutputHelper output)
    : NoRecordsSearchTestsBase(fixture, output)
{
    protected override bool UsePolling => true;
}
