using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.Domain.Models;


namespace SUI.Find.ApplicationTests.Services.QueryProvidersServiceTests;

public class QueryProvidersAsyncTests
{
    private readonly IBuildCustodianRequestService _mockBuildRequest = Substitute.For<IBuildCustodianRequestService>();
    private readonly ILogger<QueryProvidersService> _mockLogger = Substitute.For<ILogger<QueryProvidersService>>();
    private readonly IPersonIdEncryptionService _mockEncryptionService = Substitute.For<IPersonIdEncryptionService>();
    private readonly IMaskUrlService _mockMaskUrlService = Substitute.For<IMaskUrlService>();
    private readonly IOutboundAuthService _mockAuthService = Substitute.For<IOutboundAuthService>();
    private readonly QueryProvidersService _sut;

    public QueryProvidersAsyncTests()
    {
        _sut = new QueryProvidersService(
            _mockBuildRequest,
            _mockLogger,
            _mockEncryptionService,
            _mockMaskUrlService,
            _mockAuthService
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
            Connection = new ConnectionDefinition { Url = "https://test-provider.com/api/records" }
        };
    }

    [Fact]
    public async Task QueryProvidersAsync_ReturnsFail_WhenEncryptionIsNull()
    {
        // Arrange
        var input = new QueryProviderInput("client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456", MockProvider(encryption: null));

        // Act
        var result = await _sut.QueryProvidersAsync(input, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("no encryption configured", result.Error);
    }

    [Fact]
    public async Task QueryProvidersAsync_ReturnsFail_WhenEncryptionFails()
    {
        // Arrange
        var encryption = new EncryptionDefinition { Key = "test-key", KeyId = "key-1", Algorithm = "AES" };
        var input = new QueryProviderInput("client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456", MockProvider(encryption: encryption));

        _mockEncryptionService.EncryptNhsToPersonId(Arg.Any<string>(), encryption)
            .Returns(Result<string>.Fail("Encryption failed"));

        // Act
        var result = await _sut.QueryProvidersAsync(input, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Encryption failed", result.Error);
    }

    [Fact]
    public async Task QueryProvidersAsync_ReturnsFail_WhenAuthTokenFails()
    {
        // Arrange
        var encryption = new EncryptionDefinition { Key = "test-key", KeyId = "key-1", Algorithm = "AES" };
        var input = new QueryProviderInput("client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456", MockProvider(encryption: encryption));

        _mockEncryptionService.EncryptNhsToPersonId(Arg.Any<string>(), encryption)
            .Returns(Result<string>.Ok("encrypted-id"));

        _mockAuthService.GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail("Auth error"));

        // Act
        var result = await _sut.QueryProvidersAsync(input, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Auth error", result.Error);
    }

    [Fact]
    public async Task QueryProvidersAsync_ReturnsFail_WhenHttpSendFails()
    {
        // Arrange
        var encryption = new EncryptionDefinition { Key = "test-key", KeyId = "key-1", Algorithm = "AES" };
        var input = new QueryProviderInput("client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456", MockProvider(encryption: encryption));

        _mockEncryptionService.EncryptNhsToPersonId(Arg.Any<string>(), encryption)
            .Returns(Result<string>.Ok("encrypted-id"));
        _mockAuthService.GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("token"));

        _mockBuildRequest.BuildCustodianRequestAsync(Arg.Any<BuildCustodianRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail("Http connection error"));

        // Act
        var result = await _sut.QueryProvidersAsync(input, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Http connection error", result.Error);
    }

    [Fact]
    public async Task QueryProvidersAsync_ReturnsFail_WhenProviderReturnsEmptyResponse()
    {
        // Arrange
        var encryption = new EncryptionDefinition { Key = "test-key", KeyId = "key-1", Algorithm = "AES" };
        var input = new QueryProviderInput("client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456", MockProvider(encryption: encryption));

        _mockEncryptionService.EncryptNhsToPersonId(Arg.Any<string>(), encryption)
            .Returns(Result<string>.Ok("encrypted-id"));
        _mockAuthService.GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("token"));

        _mockBuildRequest.BuildCustodianRequestAsync(Arg.Any<BuildCustodianRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok(string.Empty));

        // Act
        var result = await _sut.QueryProvidersAsync(input, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Empty response", result.Error);
    }
    [Fact]
    public async Task QueryProvidersAsync_CallsGateway_WithCorrectDto()
    {
        // Arrange
        var encryption = new EncryptionDefinition { Key = "test-key", KeyId = "key-1", Algorithm = "AES" };
        var input = new QueryProviderInput("client-id-1", "job-id-1", "invocation-id", "1234567890123456", MockProvider(encryption: encryption));

        _mockEncryptionService.EncryptNhsToPersonId(Arg.Any<string>(), encryption)
            .Returns(Result<string>.Ok("encrypted-id-value"));
        _mockAuthService.GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("access-token-value"));

        _mockBuildRequest.BuildCustodianRequestAsync(Arg.Any<BuildCustodianRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("[]"));

        // Act
        await _sut.QueryProvidersAsync(input, CancellationToken.None);

        //Assert
        await _mockBuildRequest.Received(1).BuildCustodianRequestAsync(
            Arg.Is<BuildCustodianRequestDto>(req =>
                req.Provider == input.Provider &&
                req.EncryptedPersonId == "encrypted-id-value" &&
                req.AccessToken == "access-token-value"
            ),
            Arg.Any<CancellationToken>()
        );
    }
    [Fact]
    public async Task QueryProvidersAsync_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var encryption = new EncryptionDefinition { Key = "test-key", KeyId = "key-1", Algorithm = "AES" };
        var input = new QueryProviderInput("client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456", MockProvider(encryption: encryption));

        _mockEncryptionService.EncryptNhsToPersonId(Arg.Any<string>(), encryption)
            .Returns(Result<string>.Ok("encrypted-id"));
        _mockAuthService.GetAccessTokenAsync(input.Provider, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("token"));

        var searchItems = new List<SearchResultItem> { new("SystemA", "Provider A", "Type1", "/v1/records/original-id") };
        var json = JsonSerializer.Serialize(searchItems);

        _mockBuildRequest.BuildCustodianRequestAsync(Arg.Any<BuildCustodianRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok(json)); ;

        var maskedItems = new List<SearchResultItem> { new("SystemA", "Provider A", "Type1", "/v1/records/masked-id") };
        _mockMaskUrlService.CreateAsync(Arg.Any<List<SearchResultItem>>(), input, Arg.Any<CancellationToken>())
            .Returns(maskedItems);

        // Act
        var result = await _sut.QueryProvidersAsync(input, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(maskedItems, result.Value);
    }
}