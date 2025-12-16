using SUI.Transfer.Application.Services;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class RecordFinderTests
{
    [Fact]
    public async Task FindRecordsAsync_Does_FindRecords_AsExpected()
    {
        var sut = new RecordFinder();

        // ACT
        var result = await sut.FindRecordsAsync("XXX0001234", CancellationToken.None);

        // ASSERT
        result
            .Select(x => x.ProviderSystemId)
            .Distinct()
            .Should()
            .BeEquivalentTo([
                "MockSocialCareProvider",
                "MockEducationProvider",
                "MockHealthcareProvider",
                "MockCrimeDataProvider",
            ]);

        result
            .Select(x => x.ProviderName)
            .Distinct()
            .Should()
            .BeEquivalentTo([
                "SUI Custodian Stub - MockSocialCareProvider",
                "SUI Custodian Stub - MockEducationProvider",
                "SUI Custodian Stub - MockHealthcareProvider",
                "SUI Custodian Stub - MockCrimeDataProvider",
            ]);

        result
            .Select(x => x.RecordUrl)
            .Should()
            .BeEquivalentTo(
                [
                    "https://localhost:7256/api/v1/records/MockSocialCareProvider/PersonalDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockSocialCareProvider/ChildrensServicesDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockSocialCareProvider/EducationDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockSocialCareProvider/HealthDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockSocialCareProvider/CrimeDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockEducationProvider/PersonalDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockEducationProvider/ChildrensServicesDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockEducationProvider/EducationDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockEducationProvider/HealthDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockEducationProvider/CrimeDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockHealthcareProvider/PersonalDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockHealthcareProvider/ChildrensServicesDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockHealthcareProvider/EducationDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockHealthcareProvider/HealthDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockHealthcareProvider/CrimeDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockCrimeDataProvider/PersonalDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockCrimeDataProvider/ChildrensServicesDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockCrimeDataProvider/EducationDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockCrimeDataProvider/HealthDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/records/MockCrimeDataProvider/CrimeDataRecordV1/XXX0001234",
                ],
                options => options.WithStrictOrdering()
            );
    }
}
