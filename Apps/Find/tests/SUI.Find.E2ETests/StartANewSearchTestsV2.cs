namespace SUI.Find.E2ETests;

public class StartANewSearchTestsV2 : StartANewSearchTestsBase
{
    protected override bool UsePolling => true;

    public StartANewSearchTestsV2(FunctionTestFixture fixture, ITestOutputHelper output)
        : base(fixture, output) { }
}
