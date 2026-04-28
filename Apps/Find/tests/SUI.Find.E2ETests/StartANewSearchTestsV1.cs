namespace SUI.Find.E2ETests;

public class StartANewSearchTestsV1 : StartANewSearchTestsBase
{
    protected override bool UsePolling => false;

    public StartANewSearchTestsV1(FunctionTestFixture fixture, ITestOutputHelper output)
        : base(fixture, output) { }
}
