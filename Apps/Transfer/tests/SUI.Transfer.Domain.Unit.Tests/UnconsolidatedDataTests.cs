namespace SUI.Transfer.Domain.Unit.Tests;

public class UnconsolidatedDataTests
{
    [Fact]
    public void CountOfRecordsSuccessfullyFetched_Aggregates_Count_Correctly()
    {
        var sut = new UnconsolidatedData("")
        {
            ChildPersonalDetailsRecords = new ProviderRecord<ChildPersonalDetailsRecordV1>[2],
            ChildSocialCareDetailsRecords = new ProviderRecord<ChildSocialCareDetailsRecordV1>[3],
            EducationDetailsRecords = new ProviderRecord<EducationDetailsRecordV1>[4],
            ChildHealthDataRecords = new ProviderRecord<ChildHealthDataRecordV1>[5],
            ChildLinkedCrimeDataRecords = new ProviderRecord<ChildLinkedCrimeDataRecordV1>[6],
            FailedFetches = new FailedFetch[7],
        };

        // ASSERT
        Assert.Equal(2 + 3 + 4 + 5 + 6, sut.CountOfRecordsSuccessfullyFetched);
    }
}
