using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace SUI.NotificationService.Webhooks.UnitTests;

/// <summary>
/// Unit tests for WebhookSecretClient.
/// Note on NSubstitute and Azure SDK: The Azure SDK frequently uses hidden optional parameters
/// (e.g., version, outContentType) which cause standard Arg.Any() matchers to fail and throw NullReferenceExceptions.
/// To safely mock Azure clients, we use ReturnsForAnyArgs/ReceivedWithAnyArgs with "dummy" parameter values
/// to force the C# compiler to bind to the correct method overload without NSubstitute getting confused.
/// </summary>
public class WebhookSecretClientTests : IDisposable
{
    private readonly SecretClient _secretClientMock;
    private readonly MemoryCache _realMemoryCache;
    private readonly ILogger<WebhookSecretClient> _loggerMock;
    private readonly WebhookSecretClient _sut;

    public WebhookSecretClientTests()
    {
        _secretClientMock = Substitute.For<SecretClient>();
        _realMemoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = Substitute.For<ILogger<WebhookSecretClient>>();

        _sut = new WebhookSecretClient(_secretClientMock, _realMemoryCache, _loggerMock);
    }

    [Fact]
    public async Task GetSecretBase64Async_FetchesFromKeyVault_WhenCacheIsEmpty()
    {
        // Arrange
        var secretName = "supplier-a-hmac";
        var expectedSecretValue = "base64-secret-value";

        var secret = new KeyVaultSecret(secretName, expectedSecretValue);
        var response = Response.FromValue(secret, Substitute.For<Response>());

        // "dummy" forces compiler binding; ReturnsForAnyArgs bypasses strict argument matching
        _secretClientMock
            .GetSecretAsync("dummy", cancellationToken: default)
            .ReturnsForAnyArgs(Task.FromResult(response));

        // Act
        var result = await _sut.GetSecretBase64Async(secretName);

        // Assert
        Assert.Equal(expectedSecretValue, result);
        await _secretClientMock
            .ReceivedWithAnyArgs(1)
            .GetSecretAsync("dummy", cancellationToken: default);
    }

    [Fact]
    public async Task GetSecretBase64Async_ReturnsFromCache_OnSubsequentCalls()
    {
        // Arrange
        var secretName = "supplier-b-hmac";
        var expectedSecretValue = "cached-secret-value";

        var secret = new KeyVaultSecret(secretName, expectedSecretValue);
        var response = Response.FromValue(secret, Substitute.For<Response>());

        _secretClientMock
            .GetSecretAsync("dummy", cancellationToken: default)
            .ReturnsForAnyArgs(Task.FromResult(response));

        // Act - Call twice
        var firstCallResult = await _sut.GetSecretBase64Async(secretName);
        var secondCallResult = await _sut.GetSecretBase64Async(secretName);

        // Assert
        Assert.Equal(expectedSecretValue, firstCallResult);
        Assert.Equal(expectedSecretValue, secondCallResult);

        // Key Vault should only be hit exactly once due to the MemoryCache
        await _secretClientMock
            .ReceivedWithAnyArgs(1)
            .GetSecretAsync("dummy", cancellationToken: default);
    }

    [Fact]
    public async Task GetSecretBase64Async_ThrowsInvalidOperationException_WhenKeyVaultFails()
    {
        // Arrange
        var secretName = "supplier-c-hmac";
        var originalException = new RequestFailedException("Key Vault is down");

        _secretClientMock
            .GetSecretAsync("dummy", cancellationToken: default)
            .ReturnsForAnyArgs(
                Task.FromException<Azure.Response<Azure.Security.KeyVault.Secrets.KeyVaultSecret>>(
                    originalException
                )
            );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.GetSecretBase64Async(secretName)
        );

        // Verify the exception was wrapped correctly with context
        Assert.Contains("Failed to retrieve webhook secret", ex.Message);
        Assert.Equal(originalException, ex.InnerException); // Proves we didn't lose the original stack trace
    }

    public void Dispose()
    {
        _realMemoryCache.Dispose();
        GC.SuppressFinalize(this);
    }
}
