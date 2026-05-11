using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using SUI.UIHarness.Web.Models;
using SUI.UIHarness.Web.Models.Find;

namespace SUI.UIHarness.Web.Services;

public class FindService : IFindService
{
    private readonly JsonSerializerOptions _serializerSettings = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly IFindApiAuthClientProvider _authClientProvider;
    private readonly ILogger<FindService> _logger;
    private readonly string _matchApiKey;

    private static readonly string[] Scopes =
    [
        "match-record.read",
        "find-record.write",
        "find-record.read",
        "fetch-record.write",
        "fetch-record.read",
    ];

    private const string ErrorPersonId = "Error - please retry";
    private const string NotFoundPersonId = "Not Found";

    public FindService(
        IHttpClientFactory httpClientFactory,
        IFindApiAuthClientProvider authClientProvider,
        IConfiguration configuration,
        ILogger<FindService> logger
    )
    {
        _httpClient = httpClientFactory.CreateClient(nameof(FindService));
        _authClientProvider = authClientProvider;
        _matchApiKey = configuration.GetValue<string>("MATCH_API_KEY") ?? "local-dev-key-change-me";
        _logger = logger;
    }

    public async Task<FindMatchResult> MatchRecord(LocalPerson person, string clientId)
    {
        string personId;
        await GetAuthTokenAsync(clientId, Scopes);
        var matchRequest = new FindMatchRequest
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
            var requestJson = JsonSerializer.Serialize(matchRequest, _serializerSettings);
            using var content = new StringContent(
                requestJson,
                System.Text.Encoding.UTF8,
                "application/json"
            );

            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/matchperson");
            request.Headers.Authorization = _httpClient.DefaultRequestHeaders.Authorization;
            request.Headers.Add("x-api-key", _matchApiKey);
            request.Content = content;

            var result = await _httpClient.SendAsync(request);
            if (result.IsSuccessStatusCode)
            {
                return (await result.Content.ReadFromJsonAsync<FindMatchResult>())
                    ?? throw new InvalidOperationException();
            }

            personId = HandleErrorCodes(result);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "MatchRecord failed: {Message}", exception.Message);
            personId = ErrorPersonId;
        }

        return new FindMatchResult(personId);
    }

    private static string HandleErrorCodes(HttpResponseMessage result)
    {
        if (result.StatusCode == HttpStatusCode.NotFound)
            return NotFoundPersonId;

        if ((int)result.StatusCode >= 500 && (int)result.StatusCode < 600)
            return ErrorPersonId;

        return result.ReasonPhrase ?? result.StatusCode.ToString();
    }

    public async Task<string> StartSearch(string clientId, string suid, bool usePolling)
    {
        await GetAuthTokenAsync(clientId, Scopes);

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
            _logger.LogError(exception, "StartSearch failed: {Message}", exception.Message);
        }

        return string.Empty;
    }

    public async Task<SearchResultsDto> FindRecords(string clientId, string jobId, bool usePolling)
    {
        await GetAuthTokenAsync(clientId, Scopes);

        try
        {
            var result = await _httpClient.GetAsync(
                usePolling ? $"v2/searches/{jobId}/results" : $"v1/searches/{jobId}/results"
            );
            if (result.IsSuccessStatusCode)
            {
                if (usePolling)
                {
                    var searchJob = await result.Content.ReadFromJsonAsync<FindSearchResultsV2>();
                    if (searchJob != null)
                    {
                        return new SearchResultsDto(
                            searchJob.WorkItemId,
                            searchJob.Status,
                            searchJob.Items.ToArray(),
                            searchJob.CompletenessPercentage
                        );
                    }
                }
                else
                {
                    var searchJob = await result.Content.ReadFromJsonAsync<FindSearchResults>();
                    if (searchJob != null)
                    {
                        return new SearchResultsDto(
                            searchJob.JobId,
                            searchJob.Status,
                            searchJob.Items,
                            -1
                        );
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "FindRecords failed: {Message}", exception.Message);
        }

        return new SearchResultsDto("FindRecords failed", FindSearchStatus.Failed, [], 0);
    }

    public async Task<FindCustodianRecord?> FetchRecord(string clientId, string recordId)
    {
        await GetAuthTokenAsync(clientId, Scopes);

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
            _logger.LogError(exception, "FetchRecord failed: {Message}", exception.Message);
        }

        return null;
    }

    private async Task GetAuthTokenAsync(string clientId, string[] scopes)
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

        var clientSecret = _authClientProvider
            .GetAuthClients()
            .First(x => x.ClientId == clientId)
            .ClientSecret;

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
    }
}
