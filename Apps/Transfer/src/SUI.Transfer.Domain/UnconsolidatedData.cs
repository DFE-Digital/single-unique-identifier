using SUI.Custodians.API.Client;

namespace SUI.Transfer.Domain;

public record UnconsolidatedData(string Sui)
{
    public required ProviderRecord<PersonalDetailsRecordV1>[] PersonalDetailsRecords { get; init; }

    public required ProviderRecord<ChildSocialCareDetailsRecordV1>[] ChildrensServicesDetailsRecords { get; init; }

    public required ProviderRecord<EducationDetailsRecordV1>[] EducationDetailsRecords { get; init; }

    public required ProviderRecord<HealthDataRecordV1>[] HealthDataRecords { get; init; }

    public required ProviderRecord<CrimeDataRecordV1>[] CrimeDataRecords { get; init; }

    public required FailedFetch[] FailedFetches { get; init; }

    public int CountOfRecordsSuccessfullyFetched =>
        PersonalDetailsRecords.Length
        + ChildrensServicesDetailsRecords.Length
        + EducationDetailsRecords.Length
        + HealthDataRecords.Length
        + CrimeDataRecords.Length;
}
