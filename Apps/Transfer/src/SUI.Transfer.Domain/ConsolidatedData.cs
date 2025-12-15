using SUI.Custodians.API.Client;
using SUI.Transfer.Domain.Services;

namespace SUI.Transfer.Domain;

public record ConsolidatedData(string Sui)
{
    public required PersonalDetailsRecordV1Consolidated? PersonalDetailsRecord { get; init; }

    public required ChildSocialCareDetailsRecordV1Consolidated? ChildrensServicesDetailsRecord { get; init; }

    public required EducationDetailsRecordV1Consolidated? EducationDetailsRecord { get; init; }

    public required HealthDataRecordV1Consolidated? HealthDataRecord { get; init; }

    public required CrimeDataRecordV1Consolidated? CrimeDataRecord { get; init; }

    public required int CountOfRecordsSuccessfullyFetched { get; init; }

    public required FailedFetch[] FailedFetches { get; init; }
}
