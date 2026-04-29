namespace SUI.Find.E2ETests;

public class StartANewSearchTestsV1(FunctionTestFixture fixture, ITestOutputHelper output)
    : StartANewSearchTestsBase(fixture, output)
{
    protected override bool UsePolling => false;
}
