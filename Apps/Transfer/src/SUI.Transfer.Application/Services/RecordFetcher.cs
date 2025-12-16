using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Custodians.API.Client;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class RecordFetcher(HttpClient httpClient, ILogger<RecordFetcher> logger) : IRecordFetcher
{
    public Task<UnconsolidatedData> FetchRecordsAsync(
        string sui,
        RecordPointer[] recordPointers,
        CancellationToken cancellationToken
    )
    {
        var failedFetches = new ConcurrentBag<FailedFetch>();
        var chilrensServicesDetailsRecords =
            new ConcurrentBag<ProviderRecord<ChildrensServicesDetailsRecordV1>>();
        var educationRecords = new ConcurrentBag<ProviderRecord<EducationDetailsRecordV1>>();
        var personalDetailsRecords = new ConcurrentBag<ProviderRecord<PersonalDetailsRecordV1>>();
        var crimeDataRecords = new ConcurrentBag<ProviderRecord<CrimeDataRecordV1>>();
        var healthDataRecords = new ConcurrentBag<ProviderRecord<HealthDataRecordV1>>();
        recordPointers
            .AsParallel()
            .ForAll(
                async void (recordPointer) =>
                {
                    try
                    {
                        await BuildRecords(
                            recordPointer,
                            educationRecords,
                            chilrensServicesDetailsRecords,
                            personalDetailsRecords,
                            crimeDataRecords,
                            healthDataRecords,
                            cancellationToken
                        );
                    }
                    catch (Exception e)
                    {
                        failedFetches.Add(new FailedFetch(recordPointer, e.Message));
                        logger.LogError(
                            e,
                            "Failed to get record {RecordPointerRecordUrl} from provider {ProviderSystemId}",
                            recordPointer.RecordUrl,
                            recordPointer.ProviderSystemId
                        );
                    }
                }
            );
        return Task.FromResult(
            new UnconsolidatedData(sui)
            {
                PersonalDetailsRecords = personalDetailsRecords.ToArray(),
                ChildrensServicesDetailsRecords = chilrensServicesDetailsRecords.ToArray(),
                EducationDetailsRecords = educationRecords.ToArray(),
                HealthDataRecords = healthDataRecords.ToArray(),
                CrimeDataRecords = crimeDataRecords.ToArray(),
                FailedFetches = failedFetches.ToArray(),
            }
        );
    }

    private async Task BuildRecords(
        RecordPointer recordPointer,
        ConcurrentBag<ProviderRecord<EducationDetailsRecordV1>> educationRecords,
        ConcurrentBag<
            ProviderRecord<ChildrensServicesDetailsRecordV1>
        > chilrensServicesDetailsRecords,
        ConcurrentBag<ProviderRecord<PersonalDetailsRecordV1>> personalDetailsRecords,
        ConcurrentBag<ProviderRecord<CrimeDataRecordV1>> crimeDataRecords,
        ConcurrentBag<ProviderRecord<HealthDataRecordV1>> healthDataRecords,
        CancellationToken cancellationToken
    )
    {
        var result = await httpClient.GetFromJsonAsync<JsonElement>(
            recordPointer.RecordUrl,
            cancellationToken
        );
        var schemaUri = result.GetProperty("SchemaUri").GetString();
        switch (schemaUri)
        {
            case "https://schemas.example.gov.uk/sui/EducationDetailsRecordV1.json":
            {
                var parsedResult = ParsePayload<EducationDetailsRecordV1>(
                    result,
                    recordPointer.ProviderSystemId
                );
                if (parsedResult != null)
                {
                    educationRecords.Add(parsedResult);
                }

                break;
            }
            case "https://schemas.example.gov.uk/sui/ChildrensServicesDetailsRecordV1.json":
            {
                var parsedResult = ParsePayload<ChildrensServicesDetailsRecordV1>(
                    result,
                    recordPointer.ProviderSystemId
                );
                if (parsedResult != null)
                {
                    chilrensServicesDetailsRecords.Add(parsedResult);
                }

                break;
            }
            case "https://schemas.example.gov.uk/sui/PersonalDetailsRecordV1.json":
            {
                var parsedResult = ParsePayload<PersonalDetailsRecordV1>(
                    result,
                    recordPointer.ProviderSystemId
                );
                if (parsedResult != null)
                {
                    personalDetailsRecords.Add(parsedResult);
                }

                break;
            }
            case "https://schemas.example.gov.uk/sui/CrimeDataRecordV1.json":
            {
                var parsedResult = ParsePayload<CrimeDataRecordV1>(
                    result,
                    recordPointer.ProviderSystemId
                );
                if (parsedResult != null)
                {
                    crimeDataRecords.Add(parsedResult);
                }

                break;
            }
            case "https://schemas.example.gov.uk/sui/HealthDataRecordV1.json":
            {
                var parsedResult = ParsePayload<HealthDataRecordV1>(
                    result,
                    recordPointer.ProviderSystemId
                );
                if (parsedResult != null)
                {
                    healthDataRecords.Add(parsedResult);
                }

                break;
            }
            default:
            {
                throw new ArgumentException($"Invalid SchemaUri: {schemaUri}");
            }
        }
    }

    private static ProviderRecord<T>? ParsePayload<T>(JsonElement result, string providerSystemId)
        where T : class
    {
        var payload = JsonSerializer.Deserialize<T>(result.GetProperty("Payload").GetRawText());
        return payload != null ? new ProviderRecord<T>(providerSystemId, payload) : null;
    }
}
