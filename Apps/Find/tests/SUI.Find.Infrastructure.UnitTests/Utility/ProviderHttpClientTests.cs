using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Infrastructure.Utility;

namespace SUI.Find.Infrastructure.UnitTests.Utility;

public class ProviderHttpClientTests
{
    private readonly IHttpClientFactory _mockHttpClientFactory =
        Substitute.For<IHttpClientFactory>();
    private readonly ILogger<ProviderHttpClient> _mockLogger = Substitute.For<
        ILogger<ProviderHttpClient>
    >();
    private readonly ProviderHttpClient _sut;

    public ProviderHttpClientTests()
    {
        _sut = new ProviderHttpClient(_mockHttpClientFactory, _mockLogger);
    }

    [Fact]
    public async Task GetAsync_ReturnsOk_WhenResponseIsSuccess()
    {
        // Arrange
        var expectedContent = "{\"some\": \"json\"}";
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, expectedContent);
        var client = new HttpClient(mockHandler);

        _mockHttpClientFactory.CreateClient("providers").Returns(client);

        // Act
        var result = await _sut.GetAsync("http://target.url", "token", CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expectedContent, result.Value);
    }

    [Fact]
    public async Task GetAsync_ReturnsFail_WhenResponseIsNotFound()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.NotFound);
        var client = new HttpClient(mockHandler);

        _mockHttpClientFactory.CreateClient("providers").Returns(client);

        // Act
        var result = await _sut.GetAsync("http://target.url", "token", CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("NotFound", result.Error);
    }

    [Fact]
    public async Task GetAsync_ReturnsFail_WhenResponseIsServerError()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError);
        var client = new HttpClient(mockHandler);

        _mockHttpClientFactory.CreateClient("providers").Returns(client);

        // Act
        var result = await _sut.GetAsync("http://target.url", "token", CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Custodian returned unexpected status", result.Error);
    }

    private class MockHttpMessageHandler(HttpStatusCode statusCode, string content = "")
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return await Task.FromResult(
                new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content),
                }
            );
        }
    }
}
