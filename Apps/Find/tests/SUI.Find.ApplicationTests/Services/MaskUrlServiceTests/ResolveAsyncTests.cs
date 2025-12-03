using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.Domain.Models;

namespace SUI.Find.ApplicationTests.Services.MaskUrlServiceTests;

public class ResolveAsyncTests
{
    private readonly IFetchUrlStorageService _fetchUrlStorageService;
    private readonly MaskUrlService _service;

    private const string FetchId = "fetch123";
    private const string OrgId = "org1";

    public ResolveAsyncTests()
    {
        _fetchUrlStorageService = Substitute.For<IFetchUrlStorageService>();
        var logger = Substitute.For<ILogger<MaskUrlService>>();
        _service = new MaskUrlService(logger, _fetchUrlStorageService);
    }

    [Fact]
    public async Task ShouldReturnUnmaskedUrl_WhenStorageHoldsJob()
    {
        // Arrange
        var mapping = new ResolvedFetchMapping(
            TargetUrl: "https://target.com",
            TargetOrgId: "targetOrg",
            RecordType: "Health"
        );
        _fetchUrlStorageService
            .GetAsync(OrgId, FetchId, Arg.Any<CancellationToken>())
            .Returns(Result<ResolvedFetchMapping>.Ok(mapping));

        // Act
        var result = await _service.ResolveAsync(OrgId, FetchId, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(mapping.TargetUrl, result.Value.TargetUrl);
        Assert.Equal(mapping.TargetOrgId, result.Value.TargetOrgId);
        Assert.Equal(mapping.RecordType, result.Value.RecordType);
    }

    [Fact]
    public async Task ShouldReturnFail_WhenStorageReturnsNull()
    {
        // Arrange
        _fetchUrlStorageService
            .GetAsync(OrgId, FetchId, Arg.Any<CancellationToken>())
            .Returns(Result<ResolvedFetchMapping>.Ok(null));

        // Act
        var result = await _service.ResolveAsync(OrgId, FetchId, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal("Failed to resolve fetch URL", result.Error);
    }

    [Fact]
    public async Task ShouldReturnFail_WhenStorageThrowsException()
    {
        // Arrange
        _fetchUrlStorageService
            .GetAsync(OrgId, FetchId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Storage error"));

        // Act
        var result = await _service.ResolveAsync(OrgId, FetchId, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal("Failed to resolve fetch URL", result.Error);
    }
}
