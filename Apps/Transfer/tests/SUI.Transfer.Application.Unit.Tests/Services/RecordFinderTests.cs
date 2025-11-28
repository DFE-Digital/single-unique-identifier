using SUI.Transfer.Application.Services;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class RecordFinderTests
{
    [Fact]
    public async Task FindRecordsAsync_Does_FindRecords_AsExpected()
    {
        var sut = new RecordFinder();

        // ACT
        var result = await sut.FindRecordsAsync("XXX 000 1234", CancellationToken.None);

        // ASSERT
        result
            .Should()
            .AllSatisfy(recordPointer =>
                recordPointer.ProviderSystemId.Should().Be("StubCustodians")
            );

        result
            .Should()
            .AllSatisfy(recordPointer =>
                recordPointer.ProviderName.Should().Be("SUI Custodian Stubs")
            );

        result
            .Select(x => x.RecordUrl)
            .Should()
            .BeEquivalentTo([
                "https://localhost:7256/api/v1/records/ChildPersonalDetailsRecordV1/XXX0001234",
                "https://localhost:7256/api/v1/records/ChildSocialCareDetailsRecordV1/XXX0001234",
                "https://localhost:7256/api/v1/records/EducationDetailsRecordV1/XXX0001234",
                "https://localhost:7256/api/v1/records/ChildHealthDataRecordV1/XXX0001234",
                "https://localhost:7256/api/v1/records/ChildLinkedCrimeDataRecordV1/XXX0001234",
            ]);
    }
}
