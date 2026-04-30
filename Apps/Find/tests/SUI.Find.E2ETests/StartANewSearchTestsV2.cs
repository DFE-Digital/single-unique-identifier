namespace SUI.Find.E2ETests;

public class StartANewSearchTestsV2(FunctionTestFixture fixture, ITestOutputHelper output)
    : StartANewSearchTestsBase(fixture, output)
{
    protected override bool UsePolling => true;
}
