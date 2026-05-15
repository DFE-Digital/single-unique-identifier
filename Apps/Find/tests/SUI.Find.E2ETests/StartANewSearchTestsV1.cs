namespace SUI.Find.E2ETests;

[Collection(StartANewSearchTestsV1CollectionId)]
public class StartANewSearchTestsV1(FunctionTestFixture fixture, ITestOutputHelper output)
    : StartANewSearchTestsBase(fixture, output)
{
    protected override bool UsePolling => false;

    /// <summary>
    /// Because jobs in version are not distinct, they are keyed on NHS Number + Organisation ID,
    /// version 1 searches cannot be run in parallel if they use the same test data.
    /// </summary>
    public const string StartANewSearchTestsV1CollectionId = "StartANewSearchTestsV1";
}
