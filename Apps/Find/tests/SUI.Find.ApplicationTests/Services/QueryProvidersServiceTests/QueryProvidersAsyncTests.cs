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
    private readonly IMaskUrlService _mockMaskUrlService = Substitute.For<IMaskUrlService>();
    private readonly IPolicyEnforcementPoint _mockPep = Substitute.For<IPolicyEnforcementPoint>();
    private readonly QueryProvidersService _sut;

    public QueryProvidersAsyncTests()
    {
        _sut = new QueryProvidersService(
            _mockBuildRequest,
            _mockLogger,
            _mockMaskUrlService,
            _mockPep
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
    public async Task QueryProvidersAsync_CallsCustodianService_WithCorrectDto()
    {
        // Arrange
        var input = new QueryProviderInput("client-id-1", "job-id-1", "invocation-id", "1234567890123456", MockProvider());

        var searchItems = new List<SearchResultItem>();
        _mockBuildRequest.GetSearchResultItemsFromCustodianAsync(Arg.Any<BuildCustodianRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<SearchResultItem>>.Ok(searchItems));

        _mockMaskUrlService.CreateAsync(Arg.Any<List<SearchResultItem>>(), input, Arg.Any<CancellationToken>())
            .Returns(searchItems);

        // Act
        await _sut.QueryProvidersAsync(input, CancellationToken.None);

        //Assert
        await _mockBuildRequest.Received(1).GetSearchResultItemsFromCustodianAsync(
            Arg.Is<BuildCustodianRequestDto>(req =>
                req.Provider == input.Provider &&
                req.Suid == input.Suid
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task QueryProvidersAsync_ReturnsFail_WhenServiceReturnsFailure()
    {
        // Arrange
        var input = new QueryProviderInput("client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456", MockProvider());

        _mockBuildRequest.GetSearchResultItemsFromCustodianAsync(Arg.Any<BuildCustodianRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<SearchResultItem>>.Fail("Service failure"));

        // Act
        var result = await _sut.QueryProvidersAsync(input, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task QueryProvidersAsync_ReturnsFail_WhenServiceReturnsNullValue()
    {
        // Arrange
        var input = new QueryProviderInput("client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456", MockProvider());

        _mockBuildRequest.GetSearchResultItemsFromCustodianAsync(Arg.Any<BuildCustodianRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<SearchResultItem>>.Ok(null));

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
        var input = new QueryProviderInput("client-id-1",
            "job-id-1",
            "invocation-id",
            "1234567890123456", MockProvider());

        var searchItem = new SearchResultItem("SystemA", "Provider A", "Type1", "/v1/records/original-id");
        var searchItems = new List<SearchResultItem> { new("SystemA", "Provider A", "Type1", "/v1/records/original-id") };

        _mockBuildRequest.GetSearchResultItemsFromCustodianAsync(Arg.Any<BuildCustodianRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<SearchResultItem>>.Ok(searchItems));

        _mockPep.EvaluateDsaAsync(Arg.Any<PolicyCheckRequest>())
            .Returns(new PolicyDecision(true, "Allowed", "v1"));

        var maskedItems = new List<SearchResultItem> { new("SystemA", "Provider A", "Type1", "/v1/records/masked-id") };

        _mockMaskUrlService.CreateAsync(Arg.Is<List<SearchResultItem>>(items => items.Contains(searchItem)), input, Arg.Any<CancellationToken>())
            .Returns(maskedItems);

        // Act
        var result = await _sut.QueryProvidersAsync(input, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(maskedItems, result.Value);
        await _mockMaskUrlService.Received(1).CreateAsync(
            Arg.Is<List<SearchResultItem>>(x => x.Count() == 1),
            input,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryProvidersAsync_FiltersOut_DeniedRecords()
    {
        // Arrange
        var input = new QueryProviderInput("client-id-1", "job-id-1", "invocation-id", "suid-123", MockProvider());

        var allowedItem = new SearchResultItem("SysA", "ProvA", "ALLOWED_TYPE", "url1");
        var deniedItem = new SearchResultItem("SysA", "ProvA", "DENIED_TYPE", "url2");
        var items = new List<SearchResultItem> { allowedItem, deniedItem };

        _mockBuildRequest.GetSearchResultItemsFromCustodianAsync(Arg.Any<BuildCustodianRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<SearchResultItem>>.Ok(items));

        _mockPep.EvaluateDsaAsync(Arg.Is<PolicyCheckRequest>(r => r.DataType == "ALLOWED_TYPE"))
            .Returns(new PolicyDecision(true, "Allowed", "v1"));

        _mockPep.EvaluateDsaAsync(Arg.Is<PolicyCheckRequest>(r => r.DataType == "DENIED_TYPE"))
            .Returns(new PolicyDecision(false, "Denied", "v1"));

        _mockMaskUrlService.CreateAsync(Arg.Any<List<SearchResultItem>>(), input, Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<IEnumerable<SearchResultItem>>().ToList());

        // Act
        var result = await _sut.QueryProvidersAsync(input, CancellationToken.None);

        // Assert
        Assert.True(result.Success);

        // Should only contain the 1 allowed item
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal("ALLOWED_TYPE", result.Value.First().RecordType);

        // masking service also only called with one allowed item
        await _mockMaskUrlService.Received(1).CreateAsync(
            Arg.Is<List<SearchResultItem>>(list => list.Count() == 1 && list.Contains(allowedItem) && !list.Contains(deniedItem)),
            input,
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task QueryProvidersAsync_ReturnsEmpty_WhenAllRecordsDenied()
    {
        // Arrange
        var input = new QueryProviderInput("client-id-1", "job-id-1", "invocation-id", "suid-123", MockProvider());
        var deniedItem = new SearchResultItem("SysA", "ProvA", "DENIED_TYPE", "url2");

        var items = new List<SearchResultItem> { deniedItem };

        _mockBuildRequest.GetSearchResultItemsFromCustodianAsync(Arg.Any<BuildCustodianRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<SearchResultItem>>.Ok(items));

        _mockPep.EvaluateDsaAsync(Arg.Any<PolicyCheckRequest>())
            .Returns(new PolicyDecision(false, "Denied", "v1"));

        _mockMaskUrlService.CreateAsync(Arg.Any<List<SearchResultItem>>(), input, Arg.Any<CancellationToken>())
            .Returns(new List<SearchResultItem>([]));

        // Act
        var result = await _sut.QueryProvidersAsync(input, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Value);

        await _mockMaskUrlService.Received(1).CreateAsync(
            Arg.Is<List<SearchResultItem>>(list => !list.Any()),
            input,
            Arg.Any<CancellationToken>()
        );
    }
}

