using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using SUI.Custodians.API.Client;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class RecordFetcher(IHttpClientFactory httpClientFactory) : IRecordFetcher
{
    private readonly HttpClient _client = httpClientFactory.CreateClient(nameof(RecordFetcher));

    public Task<UnconsolidatedData> FetchRecordsAsync(
        string sui,
        RecordPointer[] recordPointers,
        CancellationToken cancellationToken
    )
    {
        var failedFetches = new ConcurrentBag<FailedFetch>();
        var childSocialCareDetailsRecords =
            new ConcurrentBag<ProviderRecord<ChildSocialCareDetailsRecordV1>>();
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
                            childSocialCareDetailsRecords,
                            personalDetailsRecords,
                            crimeDataRecords,
                            healthDataRecords,
                            cancellationToken
                        );
                    }
                    catch (Exception e)
                    {
                        failedFetches.Add(new FailedFetch(recordPointer, e.Message));
                    }
                }
            );
        return Task.FromResult(
            new UnconsolidatedData(sui)
            {
                ChildPersonalDetailsRecords = personalDetailsRecords.ToArray(),
                ChildSocialCareDetailsRecords = childSocialCareDetailsRecords.ToArray(),
                EducationDetailsRecords = educationRecords.ToArray(),
                ChildHealthDataRecords = healthDataRecords.ToArray(),
                ChildLinkedCrimeDataRecords = crimeDataRecords.ToArray(),
                FailedFetches = failedFetches.ToArray(),
            }
        );
    }

    private async Task BuildRecords(
        RecordPointer recordPointer,
        ConcurrentBag<ProviderRecord<EducationDetailsRecordV1>> educationRecords,
        ConcurrentBag<ProviderRecord<ChildSocialCareDetailsRecordV1>> childSocialCareDetailsRecords,
        ConcurrentBag<ProviderRecord<PersonalDetailsRecordV1>> personalDetailsRecords,
        ConcurrentBag<ProviderRecord<CrimeDataRecordV1>> crimeDataRecords,
        ConcurrentBag<ProviderRecord<HealthDataRecordV1>> healthDataRecords,
        CancellationToken cancellationToken
    )
    {
        var result = await _client.GetFromJsonAsync<JsonElement>(
            recordPointer.RecordUrl,
            cancellationToken
        );
        var schemaUri = result.GetProperty("SchemaUri").GetString();
        switch (schemaUri)
        {
            case "https://schemas.example.gov.uk/sui/EducationDetailsRecordV1.json":
            {
                var parsedResult = ParsePayload<EducationDetailsRecordV1>(result);
                if (parsedResult != null)
                {
                    educationRecords.Add(parsedResult);
                }

                break;
            }
            case "https://schemas.example.gov.uk/sui/ChildSocialCareDetailsRecordV1.json":
            {
                var parsedResult = ParsePayload<ChildSocialCareDetailsRecordV1>(result);
                if (parsedResult != null)
                {
                    childSocialCareDetailsRecords.Add(parsedResult);
                }

                break;
            }
            case "https://schemas.example.gov.uk/sui/PersonalDetailsRecordV1.json":
            {
                var parsedResult = ParsePayload<PersonalDetailsRecordV1>(result);
                if (parsedResult != null)
                {
                    personalDetailsRecords.Add(parsedResult);
                }

                break;
            }
            case "https://schemas.example.gov.uk/sui/CrimeDataRecordV1.json":
            {
                var parsedResult = ParsePayload<CrimeDataRecordV1>(result);
                if (parsedResult != null)
                {
                    crimeDataRecords.Add(parsedResult);
                }

                break;
            }
            case "https://schemas.example.gov.uk/sui/HealthDataRecordV1.json":
            {
                var parsedResult = ParsePayload<HealthDataRecordV1>(result);
                if (parsedResult != null)
                {
                    healthDataRecords.Add(parsedResult);
                }

                break;
            }
        }
    }

    private static ProviderRecord<T>? ParsePayload<T>(JsonElement result)
    {
        var payload = JsonSerializer.Deserialize<T>(result.GetProperty("Payload").GetRawText());
        return payload != null
            ? new ProviderRecord<T>(
                result.GetProperty("ProviderSystemId").GetString() ?? string.Empty,
                payload
            )
            : null;
    }
}
