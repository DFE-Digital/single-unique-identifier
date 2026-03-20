using System.Net.Http.Headers;
using System.Text.Json;
using SUI.UIHarness.Web.Models;
using SUI.UIHarness.Web.Models.Find;

namespace SUI.UIHarness.Web.Services;

public class FindService : IFindService
{
    private readonly JsonSerializerOptions _serializerSettings = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly ILogger<FindService> _logger;
    private const string? FindApiKey = "local-dev-key-change-me";
    private const string TestClientSecret = "SUIProject";
    private static readonly string[] Scopes =
    [
        "match-record.read",
        "find-record.write",
        "find-record.read",
        "fetch-record.write",
        "fetch-record.read",
    ];

    public FindService(IHttpClientFactory httpClientFactory, ILogger<FindService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(FindService));
        _logger = logger;
    }

    public async Task<FindMatchResult> MatchRecord(LocalPerson person, string clientId)
    {
        await GetAuthTokenAsync(clientId, TestClientSecret, Scopes);
        var request = new FindMatchRequest
        {
            Metadata = [],
            PersonSpecification = new FindMatchPerson
            {
                Given = person.Given,
                Family = person.Family,
                Email = person.Email,
                Phone = person.Phone,
                AddressPostalCode = person.Postcode,
                BirthDate = person.BirthDate,
                Gender = person.Gender,
            },
        };
        try
        {
            var json = JsonSerializer.Serialize(request, _serializerSettings);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var result = await _httpClient.PostAsync("v1/matchperson", content);
            if (result.IsSuccessStatusCode)
            {
                return (await result.Content.ReadFromJsonAsync<FindMatchResult>())
                    ?? throw new InvalidOperationException();
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
        }

        return new FindMatchResult("Not Found");
    }

    public async Task<string> StartSearch(string clientId, string suid, bool usePolling)
    {
        await GetAuthTokenAsync(clientId, TestClientSecret, Scopes);

        try
        {
            var request = new StartSearchRequest(suid);
            var json = JsonSerializer.Serialize(request, _serializerSettings);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var result = await _httpClient.PostAsync(
                usePolling ? "v2/searches" : "v1/searches",
                content
            );
            if (result.IsSuccessStatusCode)
            {
                if (usePolling)
                {
                    var searchJobV2 = await result.Content.ReadFromJsonAsync<FindSearchJobV2>();
                    if (searchJobV2 != null)
                    {
                        return searchJobV2.WorkItemId;
                    }
                }
                else
                {
                    var searchJob = await result.Content.ReadFromJsonAsync<FindSearchJob>();
                    if (searchJob != null)
                    {
                        return searchJob.JobId;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
        }

        return string.Empty;
    }

    public async Task<FindSearchResults> FindRecords(string clientId, string jobId, bool usePolling)
    {
        await GetAuthTokenAsync(clientId, TestClientSecret, Scopes);

        try
        {
            var result = await _httpClient.GetAsync(
                usePolling ? $"v2/searches/{jobId}/results" : $"v1/searches/{jobId}/results"
            );
            if (result.IsSuccessStatusCode)
            {
                var searchJob = await result.Content.ReadFromJsonAsync<FindSearchResults>();
                if (searchJob != null)
                {
                    return searchJob;
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
        }

        return null;
    }

    public async Task<FindCustodianRecord?> FetchRecord(string clientId, string recordId)
    {
        await GetAuthTokenAsync(clientId, TestClientSecret, Scopes);

        try
        {
            var result = await _httpClient.GetAsync($"{recordId.Remove(0, 1)}");
            if (result.IsSuccessStatusCode)
            {
                var custodianRecord = await result.Content.ReadFromJsonAsync<FindCustodianRecord>();
                if (custodianRecord != null)
                {
                    return custodianRecord;
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
        }

        return null;
    }

    private async Task GetAuthTokenAsync(string clientId, string clientSecret, string[] scopes)
    {
        var formData = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "scope", string.Join(" ", scopes) },
        };

        var content = new FormUrlEncodedContent(formData);

        var request = new HttpRequestMessage(HttpMethod.Post, "v1/auth/token")
        {
            Content = content,
        };

        var authString = $"{clientId}:{clientSecret}";
        var base64Auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authString));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Auth Failed: {response.StatusCode} - {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = result.GetProperty("access_token").GetString();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );
        if (!_httpClient.DefaultRequestHeaders.Contains("x-api-key"))
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", FindApiKey);
        }
    }
}
