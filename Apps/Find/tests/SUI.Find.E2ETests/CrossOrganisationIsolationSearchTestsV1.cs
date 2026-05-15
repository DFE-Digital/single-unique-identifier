namespace SUI.Find.E2ETests;

[Collection(StartANewSearchTestsV1.StartANewSearchTestsV1CollectionId)]
public class CrossOrganisationIsolationSearchTestsV1(
    FunctionTestFixture fixture,
    ITestOutputHelper output
) : CrossOrganisationIsolationSearchTestsBase(fixture, output)
{
    protected override bool UsePolling => false;

    [Theory]
    [MemberData(nameof(HappyPathTestData), MemberType = typeof(SearchTestsBase))]
    public async Task Search_IsIsolatedByOrganisation(TestData testData)
    {
        await RunIsolationTest(testData);
    }
}
