namespace SUI.Find.E2ETests;

public class CrossOrganisationIsolationSearchTestsV2(
    FunctionTestFixture fixture,
    ITestOutputHelper output
) : CrossOrganisationIsolationSearchTestsBase(fixture, output)
{
    protected override bool UsePolling => true;

    [Theory]
    [MemberData(nameof(HappyPathTestData), MemberType = typeof(SearchTestsBase))]
    public async Task Search_IsIsolatedByOrganisation(TestData testData)
    {
        await RunIsolationTest(testData);
    }
}
