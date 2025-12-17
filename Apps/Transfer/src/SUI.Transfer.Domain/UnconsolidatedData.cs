using SUI.Custodians.API.Client;
using SUI.Transfer.Domain.SourceGenerated;

namespace SUI.Transfer.Domain;

public record UnconsolidatedData(string Sui)
{
    public required IProviderRecord<PersonalDetailsRecordV1>[] PersonalDetailsRecords { get; init; }

    public required IProviderRecord<ChildrensServicesDetailsRecordV1>[] ChildrensServicesDetailsRecords { get; init; }

    public required IProviderRecord<EducationDetailsRecordV1>[] EducationDetailsRecords { get; init; }

    public required IProviderRecord<HealthDataRecordV1>[] HealthDataRecords { get; init; }

    public required IProviderRecord<CrimeDataRecordV1>[] CrimeDataRecords { get; init; }

    public required FailedFetch[] FailedFetches { get; init; }

    public int CountOfRecordsSuccessfullyFetched =>
        PersonalDetailsRecords.Length
        + ChildrensServicesDetailsRecords.Length
        + EducationDetailsRecords.Length
        + HealthDataRecords.Length
        + CrimeDataRecords.Length;
}
