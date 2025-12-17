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
                    "https://localhost:7256/api/v1/fetch/MockSocialCareProvider/PersonalDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockSocialCareProvider/ChildrensServicesDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockSocialCareProvider/EducationDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockSocialCareProvider/HealthDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockSocialCareProvider/CrimeDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockEducationProvider/PersonalDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockEducationProvider/ChildrensServicesDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockEducationProvider/EducationDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockEducationProvider/HealthDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockEducationProvider/CrimeDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockHealthcareProvider/PersonalDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockHealthcareProvider/ChildrensServicesDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockHealthcareProvider/EducationDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockHealthcareProvider/HealthDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockHealthcareProvider/CrimeDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockCrimeDataProvider/PersonalDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockCrimeDataProvider/ChildrensServicesDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockCrimeDataProvider/EducationDetailsRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockCrimeDataProvider/HealthDataRecordV1/XXX0001234",
                    "https://localhost:7256/api/v1/fetch/MockCrimeDataProvider/CrimeDataRecordV1/XXX0001234",
                ],
                options => options.WithStrictOrdering()
            );
    }
}
