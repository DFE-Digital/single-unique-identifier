using SUI.Custodians.API.Client;

namespace SUI.Transfer.Domain;

public record UnconsolidatedData(string Sui)
{
    public required ProviderRecord<PersonalDetailsRecordV1>[] ChildPersonalDetailsRecords { get; init; }

    public required ProviderRecord<ChildSocialCareDetailsRecordV1>[] ChildSocialCareDetailsRecords { get; init; }

    public required ProviderRecord<EducationDetailsRecordV1>[] EducationDetailsRecords { get; init; }

    public required ProviderRecord<HealthDataRecordV1>[] ChildHealthDataRecords { get; init; }

    public required ProviderRecord<CrimeDataRecordV1>[] ChildLinkedCrimeDataRecords { get; init; }

    public required FailedFetch[] FailedFetches { get; init; }

    public int CountOfRecordsSuccessfullyFetched =>
        ChildPersonalDetailsRecords.Length
        + ChildSocialCareDetailsRecords.Length
        + EducationDetailsRecords.Length
        + ChildHealthDataRecords.Length
        + ChildLinkedCrimeDataRecords.Length;
}
