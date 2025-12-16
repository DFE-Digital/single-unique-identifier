using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Custodians.API.Client;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;
using SUI.Transfer.Domain.Models;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class RecordFetcherTests
{
    private readonly ILogger<RecordFetcher> _logger = Substitute.For<ILogger<RecordFetcher>>();

    [Fact]
    public async Task FetchRecordsAsync_Does_Fetch_AsExpected()
    {
        RecordPointer[] records =
        [
            new("1234", "Education", "http://localhost:5432/api/getEducationRecord/12345"),
            new("1256", "ChildSocialCare", "http://localhost:5432/api/getSocialCareRecord/23456"),
            new(
                "2276",
                "PersonalDetails",
                "http://localhost:5432/api/getPersonalDetailsRecord/23456"
            ),
            new("3276", "HealthData", "http://localhost:5432/api/getHealthDataRecord/23456"),
            new("4276", "CrimeData", "http://localhost:5432/api/getCrimeDataRecord/23456"),
            new("0001", "BrokenProvider", "http://localhost:5432/wrongUrl"),
            new("0002", "UnknownSchemaProvider", "http://localhost:5432/unrecognisedSchema"),
        ];
        var educationRecord = new EducationDetailsRecordV1 { EducationSettingName = "Test School" };
        var socialCareRecord = new ChildrensServicesDetailsRecordV1
        {
            KeyWorker = "Test Keyworker",
        };
        var personalDetailsRecord = new PersonalDetailsRecordV1
        {
            FirstName = "John",
            LastName = "Doe",
        };
        var healthDataRecord = new HealthDataRecordV1 { RegisteredGPName = "Test GP" };
        var crimeDataRecord = new CrimeDataRecordV1 { PoliceMarkerDetails = "Test PoliceMarker" };
        var mappings = new List<MockResponse>
        {
            new()
            {
                UrlPart = "/api/getEducationRecord/12345",
                ReturnValue = System.Text.Json.JsonSerializer.Serialize(
                    new RecordEnvelope<EducationDetailsRecordV1>
                    {
                        SchemaUri = new Uri(
                            "https://schemas.example.gov.uk/sui/EducationDetailsRecordV1.json"
                        ),
                        Payload = educationRecord,
                    }
                ),
            },
            new()
            {
                UrlPart = "/api/getSocialCareRecord/23456",
                ReturnValue = System.Text.Json.JsonSerializer.Serialize(
                    new RecordEnvelope<ChildrensServicesDetailsRecordV1>
                    {
                        SchemaUri = new Uri(
                            "https://schemas.example.gov.uk/sui/ChildSocialCareDetailsRecordV1.json"
                        ),
                        Payload = socialCareRecord,
                    }
                ),
            },
            new()
            {
                UrlPart = "/api/getPersonalDetailsRecord/23456",
                ReturnValue = System.Text.Json.JsonSerializer.Serialize(
                    new RecordEnvelope<PersonalDetailsRecordV1>
                    {
                        SchemaUri = new Uri(
                            "https://schemas.example.gov.uk/sui/PersonalDetailsRecordV1.json"
                        ),
                        Payload = personalDetailsRecord,
                    }
                ),
            },
            new()
            {
                UrlPart = "/api/getHealthDataRecord/23456",
                ReturnValue = System.Text.Json.JsonSerializer.Serialize(
                    new RecordEnvelope<HealthDataRecordV1>
                    {
                        SchemaUri = new Uri(
                            "https://schemas.example.gov.uk/sui/HealthDataRecordV1.json"
                        ),
                        Payload = healthDataRecord,
                    }
                ),
            },
            new()
            {
                UrlPart = "/api/getCrimeDataRecord/23456",
                ReturnValue = System.Text.Json.JsonSerializer.Serialize(
                    new RecordEnvelope<CrimeDataRecordV1>
                    {
                        SchemaUri = new Uri(
                            "https://schemas.example.gov.uk/sui/CrimeDataRecordV1.json"
                        ),
                        Payload = crimeDataRecord,
                    }
                ),
            },
            new()
            {
                UrlPart = "/nullResponse",
                ReturnValue = System.Text.Json.JsonSerializer.Serialize(
                    new RecordEnvelope<CrimeDataRecordV1>
                    {
                        SchemaUri = new Uri(
                            "https://schemas.example.gov.uk/sui/CrimeDataRecordV1.json"
                        ),
                        Payload = null!,
                    }
                ),
            },
            new()
            {
                UrlPart = "/unrecognisedSchema",
                ReturnValue = System.Text.Json.JsonSerializer.Serialize(
                    new RecordEnvelope<CrimeDataRecordV1>
                    {
                        SchemaUri = new Uri(
                            "https://schemas.example.gov.uk/sui/unrecognisedSchema.json"
                        ),
                        Payload = null!,
                    }
                ),
            },
        };

        var client = GetClient(mappings);
        var sut = new RecordFetcher(client, _logger);

        // ACT
        var result = await sut.FetchRecordsAsync("XXX 000 1234", records, CancellationToken.None);

        // ASSERT
        result
            .Should()
            .BeEquivalentTo(
                new UnconsolidatedData("XXX 000 1234")
                {
                    PersonalDetailsRecords =
                    [
                        new ProviderRecord<PersonalDetailsRecordV1>("2276", personalDetailsRecord),
                    ],
                    ChildrensServicesDetailsRecords =
                    [
                        new ProviderRecord<ChildrensServicesDetailsRecordV1>(
                            "1256",
                            socialCareRecord
                        ),
                    ],
                    EducationDetailsRecords =
                    [
                        new ProviderRecord<EducationDetailsRecordV1>("1234", educationRecord),
                    ],
                    HealthDataRecords =
                    [
                        new ProviderRecord<HealthDataRecordV1>("3276", healthDataRecord),
                    ],
                    CrimeDataRecords =
                    [
                        new ProviderRecord<CrimeDataRecordV1>("4276", crimeDataRecord),
                    ],
                    FailedFetches =
                    [
                        new FailedFetch(
                            records.First(x =>
                                x is { ProviderName: "BrokenProvider", ProviderSystemId: "0001" }
                            ),
                            "Response status code does not indicate success: 404 (Not Found)."
                        ),
                        new FailedFetch(
                            records.First(x =>
                                x
                                    is {
                                        ProviderName: "UnknownSchemaProvider",
                                        ProviderSystemId: "0002"
                                    }
                            ),
                            "Invalid SchemaUri: https://schemas.example.gov.uk/sui/unrecognisedSchema.json"
                        ),
                    ],
                }
            );
    }

    private static HttpClient GetClient(List<MockResponse> responses)
    {
        var httpMessageHandler = Substitute.For<HttpMessageHandler>();

        httpMessageHandler
            .GetType()
            .GetMethod("SendAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(
                httpMessageHandler,
                [Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()]
            )
            .Returns(callInfo =>
            {
                // 2. Retrieve the Request Message from the arguments
                var request = callInfo.Arg<HttpRequestMessage>();
                var requestUrl = request.RequestUri?.ToString();

                // 3. Find the matching response based on the URL
                // We use Contains here to handle both relative and absolute path matching
                var match = responses.FirstOrDefault(r =>
                    requestUrl != null && requestUrl.Contains(r.UrlPart)
                );

                if (match != null)
                {
                    return Task.FromResult(
                        new HttpResponseMessage(match.StatusCode)
                        {
                            Content = new StringContent(
                                match.ReturnValue!,
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }

                // 4. Fallback if no URL matches (return 404)
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("Mock URL not found"),
                    }
                );
            });

        HttpClient httpClient = new(httpMessageHandler);
        httpClient.BaseAddress = new Uri("http://localhost:5432");

        return httpClient;
    }

    public class MockResponse
    {
        public required string UrlPart { get; init; }
        public required string ReturnValue { get; init; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    }
}
