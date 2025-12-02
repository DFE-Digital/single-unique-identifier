using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class RecordFinder : IRecordFinder
{
    private const string StubCustodiansBaseUri = "https://localhost:7256";

    private static readonly string[] StubProviderIds =
    [
        "MockSocialCareProvider",
        "MockEducationProvider",
        "MockHealthcareProvider",
        "MockCrimeDataProvider",
    ];

    private static readonly (string ProviderId, string EndpointUri)[] StubCustodiansEndpoints =
        StubProviderIds
            .SelectMany(providerId =>
                new[]
                {
                    (providerId, $"/api/v1/records/{providerId}/PersonalDetailsRecordV1"),
                    (providerId, $"/api/v1/records/{providerId}/ChildSocialCareDetailsRecordV1"),
                    (providerId, $"/api/v1/records/{providerId}/EducationDetailsRecordV1"),
                    (providerId, $"/api/v1/records/{providerId}/HealthDataRecordV1"),
                    (providerId, $"/api/v1/records/{providerId}/CrimeDataRecordV1"),
                }
            )
            .ToArray();

    public Task<RecordPointer[]> FindRecordsAsync(string sui, CancellationToken cancellationToken)
    {
        return Task.FromResult(
            StubCustodiansEndpoints
                .Select(endpoint => new RecordPointer(
                    ProviderSystemId: endpoint.ProviderId,
                    ProviderName: $"SUI Custodian Stub - {endpoint.ProviderId}",
                    RecordUrl: $"{StubCustodiansBaseUri}{endpoint.EndpointUri}/{sui}"
                ))
                .ToArray()
        );
    }
}
