namespace SUI.Find.E2ETests;

[Collection("E2E:V2 Test Collection")]
public class StartANewSearchTestsV2 : StartANewSearchTestsBase
{
    protected override bool UsePolling => true;

    public StartANewSearchTestsV2(FunctionTestFixture fixture, ITestOutputHelper output)
        : base(fixture, output) { }
}
