using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SUI.Custodians.API.Client;
using SUI.Transfer.Domain;
using SUI.Transfer.Domain.SourceGenerated;
using JsonElement = System.Text.Json.JsonElement;

namespace SUI.Transfer.Application.Services;

public class RecordFetcher(
    [FromKeyedServices(nameof(RecordFetcher))] HttpClient httpClient,
    ILogger<RecordFetcher> logger
) : IRecordFetcher
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new();

    static RecordFetcher()
    {
        JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public Task<UnconsolidatedData> FetchRecordsAsync(
        string sui,
        RecordPointer[] recordPointers,
        CancellationToken cancellationToken
    )
    {
        var failedFetches = new ConcurrentBag<FailedFetch>();
        var childrensServicesDetailsRecords =
            new ConcurrentBag<IProviderRecord<ChildrensServicesDetailsRecordV1>>();
        var educationRecords = new ConcurrentBag<IProviderRecord<EducationDetailsRecordV1>>();
        var personalDetailsRecords = new ConcurrentBag<IProviderRecord<PersonalDetailsRecordV1>>();
        var crimeDataRecords = new ConcurrentBag<IProviderRecord<CrimeDataRecordV1>>();
        var healthDataRecords = new ConcurrentBag<IProviderRecord<HealthDataRecordV1>>();
        recordPointers
            .AsParallel()
            .ForAll(
                void (recordPointer) =>
                {
                    try
                    {
                        BuildRecords(
                                recordPointer,
                                educationRecords,
                                childrensServicesDetailsRecords,
                                personalDetailsRecords,
                                crimeDataRecords,
                                healthDataRecords,
                                cancellationToken
                            )
                            .Wait(cancellationToken);
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
                ChildrensServicesDetailsRecords = childrensServicesDetailsRecords.ToArray(),
                EducationDetailsRecords = educationRecords.ToArray(),
                HealthDataRecords = healthDataRecords.ToArray(),
                CrimeDataRecords = crimeDataRecords.ToArray(),
                FailedFetches = failedFetches.ToArray(),
            }
        );
    }

    private async Task BuildRecords(
        RecordPointer recordPointer,
        ConcurrentBag<IProviderRecord<EducationDetailsRecordV1>> educationRecords,
        ConcurrentBag<
            IProviderRecord<ChildrensServicesDetailsRecordV1>
        > childrensServicesDetailsRecords,
        ConcurrentBag<IProviderRecord<PersonalDetailsRecordV1>> personalDetailsRecords,
        ConcurrentBag<IProviderRecord<CrimeDataRecordV1>> crimeDataRecords,
        ConcurrentBag<IProviderRecord<HealthDataRecordV1>> healthDataRecords,
        CancellationToken cancellationToken
    )
    {
        var result = await httpClient.GetFromJsonAsync<JsonElement>(
            recordPointer.RecordUrl,
            JsonSerializerOptions,
            cancellationToken
        );
        var schemaUri = result.GetProperty("schemaUri").GetString();
        switch (schemaUri)
        {
            case Custodians.API.Client.Models.V1.SchemaUris.EducationDetailsRecord:
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
            case Custodians.API.Client.Models.V1.SchemaUris.ChildrensServicesDetailsRecord:
            {
                var parsedResult = ParsePayload<ChildrensServicesDetailsRecordV1>(
                    result,
                    recordPointer.ProviderSystemId
                );
                if (parsedResult != null)
                {
                    childrensServicesDetailsRecords.Add(parsedResult);
                }

                break;
            }
            case Custodians.API.Client.Models.V1.SchemaUris.PersonalDetailsRecord:
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
            case Custodians.API.Client.Models.V1.SchemaUris.CrimeDataRecord:
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
            case Custodians.API.Client.Models.V1.SchemaUris.HealthDataRecord:
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
        var payload = JsonSerializer.Deserialize<T>(
            result.GetProperty("payload").GetRawText(),
            JsonSerializerOptions
        );
        return payload != null ? new ProviderRecord<T>(providerSystemId, payload) : null;
    }
}
