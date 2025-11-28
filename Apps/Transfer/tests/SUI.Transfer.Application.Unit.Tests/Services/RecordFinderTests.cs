using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

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
        Assert.Equal(
            new[]
            {
                new RecordPointer(
                    "LAC-SYSTEM-01",
                    "Example LA Case Management System",
                    "https://lac-system-01.example.gov.uk/api/records/ABC-123456"
                ),
                new RecordPointer(
                    "NHS-GP-01",
                    "Example GP Clinical System",
                    "https://nhs-gp-01.example.nhs.uk/patient/SUI-1234567890"
                ),
            },
            result
        );
    }
}
