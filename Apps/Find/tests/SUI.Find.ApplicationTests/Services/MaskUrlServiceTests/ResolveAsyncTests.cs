using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;

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
            .Returns(new ResolvedFetchMappingResult.Success(mapping));

        // Act
        var result = await _service.ResolveAsync(OrgId, FetchId, CancellationToken.None);

        // Assert
        Assert.IsType<ResolvedFetchMappingResult.Success>(result);
        var resultData = result as ResolvedFetchMappingResult.Success;
        var body = resultData!.ResolvedFetchMapping;
        Assert.NotNull(body);
        Assert.Equal(mapping.TargetUrl, body.TargetUrl);
        Assert.Equal(mapping.TargetOrgId, body.TargetOrgId);
        Assert.Equal(mapping.RecordType, body.RecordType);
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
        Assert.IsType<ResolvedFetchMappingResult.Fail>(result);
    }

    [Fact]
    public async Task ShouldReturnFail_WhenStorageFetchIsNotSuccessful()
    {
        // Arrange
        _fetchUrlStorageService
            .GetAsync(OrgId, FetchId, Arg.Any<CancellationToken>())
            .Returns(new ResolvedFetchMappingResult.Fail());

        // Act
        var result = await _service.ResolveAsync(OrgId, FetchId, CancellationToken.None);

        // Assert
        Assert.IsType<ResolvedFetchMappingResult.Fail>(result);
    }
}
