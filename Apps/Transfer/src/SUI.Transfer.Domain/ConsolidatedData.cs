using SUI.Custodians.API.Client;

namespace SUI.Transfer.Domain;

public record ConsolidatedData(string Sui)
{
    public required PersonalDetailsRecordV1? ChildPersonalDetailsRecord { get; init; }

    public required ChildSocialCareDetailsRecordV1? ChildSocialCareDetailsRecord { get; init; }

    public required EducationDetailsRecordV1? EducationDetailsRecord { get; init; }

    public required HealthDataRecordV1? ChildHealthDataRecord { get; init; }

    public required CrimeDataRecordV1? ChildLinkedCrimeDataRecord { get; init; }

    public required int CountOfRecordsSuccessfullyFetched { get; init; }

    public required FailedFetch[] FailedFetches { get; init; }
}
