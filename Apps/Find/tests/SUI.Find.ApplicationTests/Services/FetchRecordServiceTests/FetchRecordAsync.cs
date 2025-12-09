using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.Domain.Models;

namespace SUI.Find.ApplicationTests.Services.FetchRecordServiceTests;

public class FetchRecordAsyncTests
{
    private readonly ILogger<FetchRecordService> _mockLogger = Substitute.For<ILogger<FetchRecordService>>();
    private readonly IMaskUrlService _mockMaskUrlService = Substitute.For<IMaskUrlService>();
    private readonly ICustodianService _mockCustodianService = Substitute.For<ICustodianService>();
    private readonly IProviderHttpClient _mockProviderClient = Substitute.For<IProviderHttpClient>();
    private readonly IOutboundAuthService _mockOutboundAuthService = Substitute.For<IOutboundAuthService>();
    private readonly FetchRecordService _sut;

    public FetchRecordAsyncTests()
    {
        _sut = new FetchRecordService(
            _mockLogger,
            _mockMaskUrlService,
            _mockCustodianService,
            _mockProviderClient,
            _mockOutboundAuthService
        );
    }

    [Fact]
    public async Task FetchRecordAsync_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var resolvedMapping = new ResolvedFetchMapping("http://target.url", "TargetOrg", "record-type");
        _mockMaskUrlService.ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<ResolvedFetchMapping>.Ok(resolvedMapping));

        var providerDef = new ProviderDefinition
        {
            OrgId = "TargetOrg",
            Connection = new ConnectionDefinition { Auth = new AuthDefinition() }
        };
        _mockCustodianService.GetCustodianAsync("TargetOrg")
            .Returns(Result<ProviderDefinition>.Ok(providerDef));
        _mockOutboundAuthService
            .GetAccessTokenAsync(providerDef, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("access-token"));

        var expectedResult = new RecordBase(RecordId: "record-123", ProviderSystem: "provider-system", DataType: "data-type", Suid: "suid-456");

        var jsonResponse = JsonSerializer.Serialize(expectedResult);

        _mockProviderClient.GetAsync("http://target.url", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok(jsonResponse));

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expectedResult.RecordId, result.Value?.RecordId);
        Assert.Equal(expectedResult.Suid, result.Value?.Suid);
    }

    [Fact]
    public async Task FetchRecordAsync_ReturnsFail_WhenUrlServiceFails()
    {
        // Arrange
        _mockMaskUrlService.ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<ResolvedFetchMapping>.Fail("Resolution failed"));

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Resolution failed", result.Error);
    }

    [Fact]
    public async Task FetchRecordAsync_ReturnsFail_WhenCustodianNotFound()
    {
        // Arrange
        var resolvedMapping = new ResolvedFetchMapping("http://target.url", "TargetOrg", "record-type");
        _mockMaskUrlService.ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<ResolvedFetchMapping>.Ok(resolvedMapping));

        _mockCustodianService.GetCustodianAsync("TargetOrg")
            .Returns(Result<ProviderDefinition>.Fail("Custodian not found"));

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Custodian not found", result.Error);
    }

    [Fact]
    public async Task FetchRecordAsync_ReturnsFail_WhenProviderClientFails()
    {
        // Arrange
        var resolvedMapping = new ResolvedFetchMapping("http://target.url", "TargetOrg", "record-type");
        _mockMaskUrlService.ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<ResolvedFetchMapping>.Ok(resolvedMapping));

        var providerDef = new ProviderDefinition
        {
            OrgId = "TargetOrg",
            Connection = new ConnectionDefinition { Auth = new AuthDefinition() }
        };
        _mockCustodianService.GetCustodianAsync("TargetOrg")
            .Returns(Result<ProviderDefinition>.Ok(providerDef));
        _mockOutboundAuthService
            .GetAccessTokenAsync(providerDef, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Ok("access-token"));

        _mockProviderClient.GetAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail("Upstream error"));

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Upstream error", result.Error);
    }
}