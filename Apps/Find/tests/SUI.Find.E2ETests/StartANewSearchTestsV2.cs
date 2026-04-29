namespace SUI.Find.E2ETests;

[Collection("E2Ev2")]
public class StartANewSearchTestsV2 : StartANewSearchTestsBase
{
    protected override bool UsePolling => true;

    public StartANewSearchTestsV2(FunctionTestFixture fixture, ITestOutputHelper output)
        : base(fixture, output) { }
}
