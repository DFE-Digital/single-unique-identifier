using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class RecordFinder : IRecordFinder
{
    private const string StubCustodiansBaseUri = "https://localhost:7256";

    private static readonly string[] StubCustodiansEndpoints =
    [
        "/api/v1/records/ChildPersonalDetailsRecordV1",
        "/api/v1/records/ChildSocialCareDetailsRecordV1",
        "/api/v1/records/EducationDetailsRecordV1",
        "/api/v1/records/ChildHealthDataRecordV1",
        "/api/v1/records/ChildLinkedCrimeDataRecordV1",
    ];

    public Task<RecordPointer[]> FindRecordsAsync(string sui, CancellationToken cancellationToken)
    {
        sui = sui.Replace(" ", "");

        return Task.FromResult(
            StubCustodiansEndpoints
                .Select(endpointPath => new RecordPointer(
                    ProviderSystemId: "StubCustodians",
                    ProviderName: "SUI Custodian Stubs",
                    RecordUrl: $"{StubCustodiansBaseUri}{endpointPath}/{sui}"
                ))
                .ToArray()
        );
    }
}
