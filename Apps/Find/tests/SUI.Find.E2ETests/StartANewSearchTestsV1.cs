namespace SUI.Find.E2ETests;

[Collection("E2E:V1 Test Collection")]
public class StartANewSearchTestsV1 : StartANewSearchTestsBase
{
    protected override bool UsePolling => false;

    public StartANewSearchTestsV1(FunctionTestFixture fixture, ITestOutputHelper output)
        : base(fixture, output) { }
}
