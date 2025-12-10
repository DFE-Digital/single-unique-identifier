using System.Net;
using System.Reflection;
using System.Text;
using NSubstitute;
using SUI.Custodians.API.Client;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;
using SUI.Transfer.Domain.Models;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class RecordFetcherTests
{
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
        ];
        var educationRecord = new EducationDetailsRecordV1 { EducationSettingName = "Test School" };
        var socialCareRecord = new ChildSocialCareDetailsRecordV1 { KeyWorker = "Test Keyworker" };
        var personalDetailsRecord = new PersonalDetailsRecordV1
        {
            FirstName = "John",
            LastName = "Doe",
        };
        var healthDataRecord = new HealthDataRecordV1 { RegisteredGP = "Test GP" };
        var crimeDataRecord = new CrimeDataRecordV1 { PoliceMarkerDetails = "Test PoliceMarker" };
        var mappings = new List<MockResponse>
        {
            new()
            {
                UrlPart = "/api/getEducationRecord/12345",
                ReturnValue = System.Text.Json.JsonSerializer.Serialize(
                    new RecordEnvelope<EducationDetailsRecordV1>
                    {
                        ProviderSystemId = "1234",
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
                    new RecordEnvelope<ChildSocialCareDetailsRecordV1>
                    {
                        ProviderSystemId = "2345",
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
                        ProviderSystemId = "2276",
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
                        ProviderSystemId = "23456",
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
                        ProviderSystemId = "3376",
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
                        ProviderSystemId = "3376",
                        SchemaUri = new Uri(
                            "https://schemas.example.gov.uk/sui/CrimeDataRecordV1.json"
                        ),
                        Payload = null!,
                    }
                ),
            },
        };

        var client = GetClient(mappings);
        var sut = new RecordFetcher(client);

        // ACT
        var result = await sut.FetchRecordsAsync("XXX 000 1234", records, CancellationToken.None);

        // ASSERT
        result
            .Should()
            .BeEquivalentTo(
                new UnconsolidatedData("XXX 000 1234")
                {
                    ChildPersonalDetailsRecords =
                    [
                        new ProviderRecord<PersonalDetailsRecordV1>("2276", personalDetailsRecord),
                    ],
                    ChildSocialCareDetailsRecords =
                    [
                        new ProviderRecord<ChildSocialCareDetailsRecordV1>(
                            "2345",
                            socialCareRecord
                        ),
                    ],
                    EducationDetailsRecords =
                    [
                        new ProviderRecord<EducationDetailsRecordV1>("1234", educationRecord),
                    ],
                    ChildHealthDataRecords =
                    [
                        new ProviderRecord<HealthDataRecordV1>("23456", healthDataRecord),
                    ],
                    ChildLinkedCrimeDataRecords =
                    [
                        new ProviderRecord<CrimeDataRecordV1>("3376", crimeDataRecord),
                    ],
                    FailedFetches =
                    [
                        new FailedFetch(
                            records.First(x =>
                                x is { ProviderName: "BrokenProvider", ProviderSystemId: "0001" }
                            ),
                            "Response status code does not indicate success: 404 (Not Found)."
                        ),
                    ],
                }
            );
    }

    private static IHttpClientFactory GetClient(List<MockResponse> responses)
    {
        var httpMessageHandler = Substitute.For<HttpMessageHandler>();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();

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

        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        return httpClientFactory;
    }

    public class MockResponse
    {
        public required string UrlPart { get; init; }
        public required string ReturnValue { get; init; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    }
}
