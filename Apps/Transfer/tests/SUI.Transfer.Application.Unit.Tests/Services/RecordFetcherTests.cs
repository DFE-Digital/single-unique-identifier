using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class RecordFetcherTests
{
    [Fact]
    public async Task FetchRecordsAsync_Does_Fetch_AsExpected()
    {
        var sut = new RecordFetcher();

        // ACT
        var result = await sut.FetchRecordsAsync("XXX 000 1234", [], CancellationToken.None);

        // ASSERT
        result
            .Should()
            .BeEquivalentTo(
                new UnconsolidatedData("XXX 000 1234")
                {
                    ChildPersonalDetailsRecords = [],
                    ChildSocialCareDetailsRecords = [],
                    EducationDetailsRecords = [],
                    ChildHealthDataRecords = [],
                    ChildLinkedCrimeDataRecords = [],
                    FailedFetches = [],
                }
            );
    }
}
