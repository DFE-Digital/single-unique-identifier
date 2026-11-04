using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using SUI.UIHarness.Web.Models;

namespace SUI.UIHarness.Web.Services;

public class GetAnIdentifierService : IGetAnIdentifierService
{
    private readonly JsonSerializerOptions _serializerSettings = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly IGetAnIdentifierAuthClientProvider _authClientProvider;
    private readonly ILogger<GetAnIdentifierService> _logger;
    private readonly string _matchApiKey;
    private readonly string _inboundAccessTokenUrl;
    private readonly string? _gatewayAuthScope;

    private const string ErrorPersonId = "Error";
    private const string NotFoundPersonId = "Not Found";

    public GetAnIdentifierService(
        IHttpClientFactory httpClientFactory,
        IGetAnIdentifierAuthClientProvider authClientProvider,
        IConfiguration configuration,
        ILogger<GetAnIdentifierService> logger
    )
    {
        _httpClient = httpClientFactory.CreateClient(nameof(GetAnIdentifierService));
        _authClientProvider = authClientProvider;
        _matchApiKey =
            configuration.GetValue<string>(ConfigKeys.MatchApiKey) ?? "local-dev-key-change-me";
        _inboundAccessTokenUrl =
            configuration.GetValue<string>(ConfigKeys.AccessTokenUrl)
            ?? throw new InvalidOperationException(
                $"{ConfigKeys.AccessTokenUrl} configuration is missing"
            );
        _gatewayAuthScope = configuration.GetValue<string>(ConfigKeys.FindApiGatewayAuthScope);
        _logger = logger;
    }

    public async Task<PersonMatch> GetAnId(LocalPerson person, string clientId)
    {
        string personId;
        await GetAuthTokenAsync(clientId);
        var matchRequest = new GetAnIdentifierRequest
        {
            PersonSpecification = new PersonSpecification
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

            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/get-an-identifier");
            request.Headers.Authorization = _httpClient.DefaultRequestHeaders.Authorization;
            request.Headers.Add("x-api-key", _matchApiKey);
            request.Content = content;

            var result = await _httpClient.SendAsync(request);
            if (result.IsSuccessStatusCode)
            {
                return (await result.Content.ReadFromJsonAsync<PersonMatch>())
                    ?? throw new InvalidOperationException();
            }

            personId = HandleErrorCodes(result);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "MatchRecord failed: {Message}", exception.Message);
            personId = ErrorPersonId;
        }

        return new PersonMatch(personId, []);
    }

    private static string HandleErrorCodes(HttpResponseMessage result)
    {
        if (result.StatusCode == HttpStatusCode.NotFound)
            return NotFoundPersonId;

        var statusMessage = $"{(int)result.StatusCode} {result.ReasonPhrase}";

        return $"{ErrorPersonId} ({statusMessage})";
    }

    private async Task GetAuthTokenAsync(string clientId)
    {
        var formData = new Dictionary<string, string?>
        {
            { "grant_type", "client_credentials" },
            { "scope", _gatewayAuthScope },
        };

        var content = new FormUrlEncodedContent(formData);

        using var request = new HttpRequestMessage(HttpMethod.Post, _inboundAccessTokenUrl);
        request.Content = content;

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
