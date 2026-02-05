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
    private readonly IBuildCustodianRequestService _mockBuildRequest =
        Substitute.For<IBuildCustodianRequestService>();
    private readonly ILogger<QueryProvidersService> _mockLogger = Substitute.For<
        ILogger<QueryProvidersService>
    >();
    private readonly IMaskUrlService _mockMaskUrlService = Substitute.For<IMaskUrlService>();
    private readonly QueryProvidersService _sut;

    public QueryProvidersAsyncTests()
    {
        _sut = new QueryProvidersService(_mockBuildRequest, _mockLogger, _mockMaskUrlService);
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
    public async Task QueryProvidersAsync_CallsCustodianService_WithCorrectDto()
    {
        // Arrange
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider()
        );

        var searchItems = new List<CustodianSearchResultItem>();
        _mockBuildRequest
            .GetSearchResultItemsFromCustodianAsync(
                Arg.Any<BuildCustodianRequestDto>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Result<List<CustodianSearchResultItem>>.Ok(searchItems));

        _mockMaskUrlService
            .CreateAsync(
                Arg.Any<List<CustodianSearchResultItem>>(),
                input,
                Arg.Any<CancellationToken>()
            )
            .Returns(searchItems);

        // Act
        await _sut.QueryProvidersAsync(input, CancellationToken.None);

        //Assert
        await _mockBuildRequest
            .Received(1)
            .GetSearchResultItemsFromCustodianAsync(
                Arg.Is<BuildCustodianRequestDto>(req =>
                    req.Provider == input.Provider && req.Suid == input.Suid
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task QueryProvidersAsync_ReturnsFail_WhenServiceReturnsFailure()
    {
        // Arrange
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider()
        );

        _mockBuildRequest
            .GetSearchResultItemsFromCustodianAsync(
                Arg.Any<BuildCustodianRequestDto>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Result<List<CustodianSearchResultItem>>.Fail("Service failure"));

        // Act
        var result = await _sut.QueryProvidersAsync(input, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task QueryProvidersAsync_ReturnsFail_WhenServiceReturnsNullValue()
    {
        // Arrange
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider()
        );

        _mockBuildRequest
            .GetSearchResultItemsFromCustodianAsync(
                Arg.Any<BuildCustodianRequestDto>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Result<List<CustodianSearchResultItem>>.Ok(null));

        // Act
        var result = await _sut.QueryProvidersAsync(input, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("searchResultItemsResponse returned null", result.Error);
    }

    [Fact]
    public async Task QueryProvidersAsync_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var input = new QueryProviderInput(
            "client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456",
            MockProvider()
        );

        var searchItems = new List<CustodianSearchResultItem>
        {
            new("test-org-1", "Type1", "/v1/records/original-id", "SystemA", "TestRecord"),
        };

        _mockBuildRequest
            .GetSearchResultItemsFromCustodianAsync(
                Arg.Any<BuildCustodianRequestDto>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Result<List<CustodianSearchResultItem>>.Ok(searchItems));

        var maskedItems = new List<CustodianSearchResultItem>
        {
            new("test-org-1", "Type1", "/v1/records/masked-id", "SystemA", "TestRecord"),
        };
        _mockMaskUrlService
            .CreateAsync(
                Arg.Any<List<CustodianSearchResultItem>>(),
                input,
                Arg.Any<CancellationToken>()
            )
            .Returns(maskedItems);

        // Act
        var result = await _sut.QueryProvidersAsync(input, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(maskedItems, result.Value);
        await _mockMaskUrlService
            .Received(1)
            .CreateAsync(searchItems, input, Arg.Any<CancellationToken>());
    }
}
