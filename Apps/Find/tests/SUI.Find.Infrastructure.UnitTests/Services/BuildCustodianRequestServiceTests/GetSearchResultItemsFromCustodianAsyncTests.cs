using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services.BuildCustodianRequestServiceTests;

public class GetSearchResultItemsFromCustodianAsyncTests
{
    private readonly ILogger<BuildCustodianRequestsService> _mockLogger = Substitute.For<
        ILogger<BuildCustodianRequestsService>
    >();
    private readonly IBuildCustodianHttpRequest _mockRequestBuilder =
        Substitute.For<IBuildCustodianHttpRequest>();
    private readonly IProviderHttpClient _mockHttpClient = Substitute.For<IProviderHttpClient>();
    private readonly IOutboundAuthService _mockAuthService = Substitute.For<IOutboundAuthService>();
    private readonly BuildCustodianRequestsService _sut;

    // Extracted to a constant for cleaner, safer mock assertions
    private const string TestNhsNumber = "1234567890";

    public GetSearchResultItemsFromCustodianAsyncTests()
    {
        _sut = new BuildCustodianRequestsService(
            _mockLogger,
            _mockRequestBuilder,
            _mockHttpClient,
            _mockAuthService
        );
    }

    private static ProviderDefinition MockProvider(string orgId = "test-org-1")
    {
        return new ProviderDefinition
        {
            OrgId = orgId,
            ProviderName = "Test Provider",
            Connection = new ConnectionDefinition { Url = "https://test-provider.com/api/records" },
        };
    }

    [Fact]
    public async Task GetSearchResultItemsFromCustodianAsync_ReturnsSuccess_WhenEncryptionIsNull()
    {
        // Arrange
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider()
        );
        var request = new BuildCustodianRequestDto(input.Provider, TestNhsNumber);
        using var httpRequestMessage = new HttpRequestMessage();

        _mockAuthService
            .GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("valid-token"));

        _mockRequestBuilder
            .BuildHttpRequest(input.Provider, TestNhsNumber, "valid-token")
            .Returns(httpRequestMessage);

        var expectedItems = new List<SearchResultItem>
        {
            new()
            {
                RecordType = "Type1",
                RecordUrl = "/v1/records/original-id",
                SystemId = "SystemA",
                RecordId = "TestRecord",
            },
        };

        var jsonResponse = JsonSerializer.Serialize(expectedItems, JsonSerializerOptions.Web);

        _mockHttpClient
            .SendAsync(httpRequestMessage, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok(jsonResponse));

        // Act
        var result = await _sut.GetSearchResultItemsFromCustodianAsync(
            request,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("/v1/records/original-id", result.Value[0].RecordUrl);

        _mockRequestBuilder
            .Received(1)
            .BuildHttpRequest(input.Provider, TestNhsNumber, "valid-token");
        await _mockHttpClient
            .Received(1)
            .SendAsync(httpRequestMessage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSearchResultItemsFromCustodianAsync_ReturnsFail_WhenAuthTokenFails()
    {
        // Arrange
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider()
        );
        var request = new BuildCustodianRequestDto(input.Provider, TestNhsNumber);

        _mockAuthService
            .GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail("Auth error"));

        // Act
        var result = await _sut.GetSearchResultItemsFromCustodianAsync(
            request,
            CancellationToken.None
        );

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Auth error", result.Error);
        _mockRequestBuilder.DidNotReceiveWithAnyArgs().BuildHttpRequest(null!, null!, null);
    }

    [Fact]
    public async Task GetSearchResultItemsFromCustodianAsync_ReturnsFail_WhenHttpSendFails()
    {
        // Arrange
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider()
        );
        var request = new BuildCustodianRequestDto(input.Provider, TestNhsNumber);
        using var httpRequestMessage = new HttpRequestMessage();

        _mockAuthService
            .GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("valid-token"));

        // FIXED: Expecting raw NhsNumber instead of "encrypted-id"
        _mockRequestBuilder
            .BuildHttpRequest(input.Provider, TestNhsNumber, "valid-token")
            .Returns(httpRequestMessage);

        _mockHttpClient
            .SendAsync(httpRequestMessage, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail("Http connection error"));

        // Act
        var result = await _sut.GetSearchResultItemsFromCustodianAsync(
            request,
            CancellationToken.None
        );

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Http connection error", result.Error);
    }

    [Fact]
    public async Task GetSearchResultItemsFromCustodianAsync_ReturnsFail_WhenProviderReturnsEmptyResponse()
    {
        // Arrange
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider()
        );
        var request = new BuildCustodianRequestDto(input.Provider, TestNhsNumber);
        using var httpRequestMessage = new HttpRequestMessage();

        _mockAuthService
            .GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("valid-token"));

        // FIXED: Expecting raw NhsNumber instead of "encrypted-id"
        _mockRequestBuilder
            .BuildHttpRequest(input.Provider, TestNhsNumber, "valid-token")
            .Returns(httpRequestMessage);

        _mockHttpClient
            .SendAsync(httpRequestMessage, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok(string.Empty));

        // Act
        var result = await _sut.GetSearchResultItemsFromCustodianAsync(
            request,
            CancellationToken.None
        );

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Empty response", result.Error);
    }

    [Fact]
    public async Task GetSearchResultItemsFromCustodianAsync_ReturnsFail_WhenProviderReturnsEmptyList()
    {
        // Arrange
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider()
        );
        var request = new BuildCustodianRequestDto(input.Provider, TestNhsNumber);
        using var httpRequestMessage = new HttpRequestMessage();

        _mockAuthService
            .GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("valid-token"));

        // FIXED: Expecting raw NhsNumber instead of "encrypted-id"
        _mockRequestBuilder
            .BuildHttpRequest(input.Provider, TestNhsNumber, "valid-token")
            .Returns(httpRequestMessage);

        _mockHttpClient
            .SendAsync(httpRequestMessage, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("[]"));

        // Act
        var result = await _sut.GetSearchResultItemsFromCustodianAsync(
            request,
            CancellationToken.None
        );

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No search result items", result.Error);
    }

    [Fact]
    public async Task GetSearchResultItemsFromCustodianAsync_ReturnsOk_WhenProviderReturnsValidItems()
    {
        // Arrange
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider()
        );
        var request = new BuildCustodianRequestDto(input.Provider, TestNhsNumber);
        using var httpRequestMessage = new HttpRequestMessage();

        _mockAuthService
            .GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("valid-token"));

        // FIXED: Expecting raw NhsNumber instead of "encrypted-id"
        _mockRequestBuilder
            .BuildHttpRequest(input.Provider, TestNhsNumber, "valid-token")
            .Returns(httpRequestMessage);

        var expectedItems = new List<SearchResultItem>
        {
            new()
            {
                RecordType = "Type1",
                RecordUrl = "/v1/records/original-id",
                SystemId = "SystemA",
                RecordId = "TestRecord",
            },
        };

        var jsonResponse = JsonSerializer.Serialize(expectedItems, JsonSerializerOptions.Web);

        _mockHttpClient
            .SendAsync(httpRequestMessage, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok(jsonResponse));

        // Act
        var result = await _sut.GetSearchResultItemsFromCustodianAsync(
            request,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("/v1/records/original-id", result.Value[0].RecordUrl);

        // FIXED: Updated assertion tracking
        _mockRequestBuilder
            .Received(1)
            .BuildHttpRequest(input.Provider, TestNhsNumber, "valid-token");
        await _mockHttpClient
            .Received(1)
            .SendAsync(httpRequestMessage, Arg.Any<CancellationToken>());
    }
}
