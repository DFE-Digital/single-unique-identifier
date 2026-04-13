using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SUI.Find.Application.Configurations;
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
    private readonly IPersonIdEncryptionService _mockEncryptionService =
        Substitute.For<IPersonIdEncryptionService>();
    private readonly IOptions<EncryptionConfiguration> _encryptionConfig = Substitute.For<
        IOptions<EncryptionConfiguration>
    >();
    private readonly BuildCustodianRequestsService _sut;

    public GetSearchResultItemsFromCustodianAsyncTests()
    {
        _encryptionConfig.Value.Returns(
            new EncryptionConfiguration { EnablePersonIdEncryption = true }
        );
        _sut = new BuildCustodianRequestsService(
            _mockLogger,
            _mockRequestBuilder,
            _mockHttpClient,
            _mockAuthService,
            _mockEncryptionService,
            _encryptionConfig
        );
    }

    private static ProviderDefinition MockProvider(
        string orgId = "test-org-1",
        EncryptionDefinition? encryption = null
    )
    {
        return new ProviderDefinition
        {
            OrgId = orgId,
            ProviderName = "Test Provider",
            Encryption = encryption,
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
            MockProvider(encryption: null)
        );
        var request = new BuildCustodianRequestDto(input.Provider, "1234567890");
        using var httpRequestMessage = new HttpRequestMessage();

        _mockEncryptionService
            .EncryptNhsToPersonId(Arg.Any<string>(), Arg.Any<EncryptionDefinition>())
            .Returns(Result<string>.Ok("encrypted-id"));

        _mockAuthService
            .GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("valid-token"));

        _mockRequestBuilder
            .BuildHttpRequest(input.Provider, "1234567890", "valid-token")
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
            .BuildHttpRequest(input.Provider, "1234567890", "valid-token");
        await _mockHttpClient
            .Received(1)
            .SendAsync(httpRequestMessage, Arg.Any<CancellationToken>());
        _mockEncryptionService.DidNotReceiveWithAnyArgs().EncryptNhsToPersonId(null!, null!);
    }

    [Fact]
    public async Task GetSearchResultItemsFromCustodianAsync_ReturnsFail_WhenEncryptionFails()
    {
        // Arrange
        var encryption = new EncryptionDefinition
        {
            Key = "test-key",
            KeyId = "key-1",
            Algorithm = "AES",
        };
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider(encryption: encryption)
        );
        var request = new BuildCustodianRequestDto(input.Provider, "1234567890");

        _mockEncryptionService
            .EncryptNhsToPersonId(request.Suid, input.Provider.Encryption!)
            .Returns(Result<string>.Fail("Encryption failed"));

        // Act
        var result = await _sut.GetSearchResultItemsFromCustodianAsync(
            request,
            CancellationToken.None
        );

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Encryption failed", result.Error);
        await _mockAuthService
            .DidNotReceiveWithAnyArgs()
            .GetAccessTokenAsync(null!, CancellationToken.None);
    }

    [Fact]
    public async Task GetSearchResultItemsFromCustodianAsync_ReturnsFail_WhenAuthTokenFails()
    {
        // Arrange
        var encryption = new EncryptionDefinition
        {
            Key = "test-key",
            KeyId = "key-1",
            Algorithm = "AES",
        };
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider(encryption: encryption)
        );
        var request = new BuildCustodianRequestDto(input.Provider, "1234567890");

        _mockEncryptionService
            .EncryptNhsToPersonId(Arg.Any<string>(), Arg.Any<EncryptionDefinition>())
            .Returns(Result<string>.Ok("encrypted-id"));

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
        var encryption = new EncryptionDefinition
        {
            Key = "test-key",
            KeyId = "key-1",
            Algorithm = "AES",
        };
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider(encryption: encryption)
        );
        var request = new BuildCustodianRequestDto(input.Provider, "1234567890");
        using var httpRequestMessage = new HttpRequestMessage();

        _mockEncryptionService
            .EncryptNhsToPersonId(Arg.Any<string>(), Arg.Any<EncryptionDefinition>())
            .Returns(Result<string>.Ok("encrypted-id"));

        _mockAuthService
            .GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("valid-token"));

        _mockRequestBuilder
            .BuildHttpRequest(input.Provider, "encrypted-id", "valid-token")
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
        var encryption = new EncryptionDefinition
        {
            Key = "test-key",
            KeyId = "key-1",
            Algorithm = "AES",
        };
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider(encryption: encryption)
        );
        var request = new BuildCustodianRequestDto(input.Provider, "1234567890");
        using var httpRequestMessage = new HttpRequestMessage();

        _mockEncryptionService
            .EncryptNhsToPersonId(Arg.Any<string>(), Arg.Any<EncryptionDefinition>())
            .Returns(Result<string>.Ok("encrypted-id"));

        _mockAuthService
            .GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("valid-token"));

        _mockRequestBuilder
            .BuildHttpRequest(input.Provider, "encrypted-id", "valid-token")
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
        var encryption = new EncryptionDefinition
        {
            Key = "test-key",
            KeyId = "key-1",
            Algorithm = "AES",
        };
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider(encryption: encryption)
        );
        var request = new BuildCustodianRequestDto(input.Provider, "1234567890");
        using var httpRequestMessage = new HttpRequestMessage();

        _mockEncryptionService
            .EncryptNhsToPersonId(Arg.Any<string>(), Arg.Any<EncryptionDefinition>())
            .Returns(Result<string>.Ok("encrypted-id"));

        _mockAuthService
            .GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("valid-token"));

        _mockRequestBuilder
            .BuildHttpRequest(input.Provider, "encrypted-id", "valid-token")
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
        var encryption = new EncryptionDefinition
        {
            Key = "test-key",
            KeyId = "key-1",
            Algorithm = "AES",
        };
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider(encryption: encryption)
        );
        var request = new BuildCustodianRequestDto(input.Provider, "1234567890");
        using var httpRequestMessage = new HttpRequestMessage();

        _mockEncryptionService
            .EncryptNhsToPersonId(Arg.Any<string>(), Arg.Any<EncryptionDefinition>())
            .Returns(Result<string>.Ok("encrypted-id"));

        _mockAuthService
            .GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("valid-token"));

        _mockRequestBuilder
            .BuildHttpRequest(input.Provider, "encrypted-id", "valid-token")
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
            .BuildHttpRequest(input.Provider, "encrypted-id", "valid-token");
        await _mockHttpClient
            .Received(1)
            .SendAsync(httpRequestMessage, Arg.Any<CancellationToken>());
    }
}
