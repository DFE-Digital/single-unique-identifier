using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SUI.Find.Infrastructure.Constants;
using SUI.Find.Infrastructure.Interfaces.Fhir;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Models.Fhir;

namespace SUI.Find.Infrastructure.Services.Fhir;

public sealed class FhirAuthTokenService(
    IOptions<AuthTokenServiceConfig> options,
    ILogger<FhirAuthTokenService> logger,
    IHttpClientFactory httpClientFactory
) : IFhirAuthTokenService, IDisposable
{
    private static readonly JsonWebTokenHandler TokenHandler = new();
    private readonly SemaphoreSlim _renewalLock = new(1, 1);
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("nhs-auth-api");
    private readonly AuthTokenServiceConfig _options = options.Value;
    private volatile CachedToken? _cachedToken;
    private string? _privateKey;
    private string? _clientId;
    private string? _kid;

    /// <summary>
    /// Retrieves a valid bearer token, handling caching and renewal automatically.
    /// This method is thread-safe.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A valid access token.</returns>
    public async Task<string> GetBearerToken(CancellationToken cancellationToken)
    {
        if (_cachedToken?.IsValid() == true)
        {
            return _cachedToken.AccessToken;
        }

        await _renewalLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken?.IsValid() == true)
            {
                return _cachedToken.AccessToken;
            }

            logger.LogInformation("Cached token is expired or missing. Proceeding with renewal.");

            EnsureInitializedAsync();

            var newCachedToken = await FetchNewAccessTokenAsync(cancellationToken);
#pragma warning disable CS0420
            Volatile.Write(ref _cachedToken, newCachedToken);
#pragma warning restore CS0420

            logger.LogInformation("Successfully obtained and cached new access token.");
            return newCachedToken.AccessToken;
        }
        finally
        {
            _renewalLock.Release();
        }
    }

    private void EnsureInitializedAsync()
    {
        if (_privateKey is not null)
            return;

        logger.LogInformation(
            $"{nameof(FhirAuthTokenService)} Initializing. Loading NHS FHIR secrets from configuration..."
        );

        _privateKey =
            _options.NHS_DIGITAL_PRIVATE_KEY
            ?? throw new ArgumentNullException(_options.NHS_DIGITAL_PRIVATE_KEY);
        _clientId =
            _options.NHS_DIGITAL_CLIENT_ID
            ?? throw new ArgumentNullException(_options.NHS_DIGITAL_CLIENT_ID);
        _kid =
            _options.NHS_DIGITAL_KID ?? throw new ArgumentNullException(_options.NHS_DIGITAL_KID);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                $$"""{{nameof(FhirAuthTokenService)}} Initializing. NHS_DIGITAL_PRIVATE_KEY length is: {KeyLength}""",
                _privateKey?.Length
            );

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                $$"""{{nameof(FhirAuthTokenService)}} Initializing. NHS_DIGITAL Key details are: {KeyDetails}""",
                new { clientId = _clientId, kid = _kid }
            );
    }

    private async Task<CachedToken> FetchNewAccessTokenAsync(CancellationToken cancellationToken)
    {
        var authAddress = _httpClient.BaseAddress!.ToString();
        var tokenExpiresInMinutes =
            _options.NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES
            ?? FhirConstants.AccountTokenExpiresInMinutes;

        var clientAssertion = GenerateClientAssertionJwt(authAddress, tokenExpiresInMinutes);

        var requestBody = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                {
                    "client_assertion_type",
                    "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"
                },
                { "client_assertion", clientAssertion },
            }
        );

        logger.LogDebug("Requesting new access token from {TokenEndpoint}", authAddress);

        var response = await _httpClient.PostAsync(authAddress, requestBody, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Authentication failed with status code {StatusCode}. Response: {ErrorContent}",
                response.StatusCode,
                errorContent
            );
            throw new HttpRequestException(
                $"Authentication failed. Status: {response.StatusCode}, Body: {errorContent}",
                null,
                response.StatusCode
            );
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsedJson = JsonNode.Parse(responseBody);

        var accessToken =
            parsedJson?["access_token"]?.ToString()
            ?? throw new InvalidOperationException("Response did not contain an 'access_token'.");

        var hasExpiry = int.TryParse(parsedJson["expires_in"]!.ToString(), out var expiresIn);
        var expirationTime = DateTimeOffset.UtcNow.AddSeconds(
            hasExpiry ? expiresIn : tokenExpiresInMinutes
        );

        return new CachedToken(accessToken, expirationTime);
    }

    private string GenerateClientAssertionJwt(string audience, int expInMinutes)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = audience,
            Issuer = _clientId!,
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, _clientId!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
            Expires = DateTime.UtcNow.AddMinutes(expInMinutes),
            SigningCredentials = CreateSigningCredentials(_privateKey!, _kid!),
        };
        return TokenHandler.CreateToken(tokenDescriptor);
    }

    private static SigningCredentials CreateSigningCredentials(string privateKey, string kid)
    {
        var rsa = RSA.Create();

        var keyContents = privateKey
            .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
            .Replace("-----END RSA PRIVATE KEY-----", "")
            .ReplaceLineEndings("")
            .Trim();

        rsa.ImportRSAPrivateKey(Convert.FromBase64String(keyContents), out _);
        var securityKey = new RsaSecurityKey(rsa) { KeyId = kid };

        return new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha512)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false },
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renewalLock.Dispose();
        }
    }
}
