using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;

namespace SUI.Find.Application.UnitTests.Services.MaskUrlServiceTests;

public class CreateAsyncTests
{
    private readonly ILogger<MaskUrlService> _logger = Substitute.For<ILogger<MaskUrlService>>();
    private readonly IFetchUrlStorageService _fetchUrlStorageService =
        Substitute.For<IFetchUrlStorageService>();

    [Fact]
    public async Task ReturnsMaskedUrls_ForValidInput()
    {
        // Arrange
        var originalUrl = "https://localhost.example.com/somewhere";
        List<CustodianSearchResultItem> items =
        [
            new("test-custodian", "Health", originalUrl, "bib", "bob"),
            new("test-custodian", "Education", originalUrl, "ll", "TestRecord"),
        ];
        var providerDefinition = new ProviderDefinition();
        var queryProviderInput = new QueryProviderInput(
            "requestingOrg",
            "job-id",
            "correlationId",
            "suid",
            providerDefinition
        );

        // Act
        var service = new MaskUrlService(_logger, _fetchUrlStorageService);
        var result = await service.CreateAsync(items, queryProviderInput, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(
            result,
            url =>
            {
                var guid = url.RecordUrl.Split("/").Last();
                var isGUid = Guid.TryParseExact(guid, "N", out _);
                Assert.True(isGUid);
                Assert.DoesNotContain("localhost.example.com", url.RecordUrl);
            }
        );
    }

    [Fact]
    public async Task ReturnsEmptyList_ForEmptyInput()
    {
        // Arrange
        var providerDefinition = new ProviderDefinition();
        var queryProviderInput = new QueryProviderInput(
            "requestingOrg",
            "job-id",
            "correlationId",
            "suid",
            providerDefinition
        );
        var service = new MaskUrlService(_logger, _fetchUrlStorageService);

        // Act
        var result = await service.CreateAsync([], queryProviderInput, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ShouldOnlyReturnsMaskedUrls_WhenExceptionOccurs()
    {
        // Arrange
        var originalUrl = "https://localhost.example.com/somewhere";
        List<CustodianSearchResultItem> items =
        [
            new("test-custodian", "Health", originalUrl, "bib", "bob"),
            new("test-custodian", "Education", originalUrl, "ll", "TestRecord"),
        ];
        var providerDefinition = new ProviderDefinition();
        var queryProviderInput = new QueryProviderInput(
            "requestingOrg",
            "job-id",
            "correlationId",
            "suid",
            providerDefinition
        );
        _fetchUrlStorageService
            .AddAsync(Arg.Any<AddFetchUrlRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask, Task.FromException(new Exception("Simulated failure")));

        // Act
        var service = new MaskUrlService(_logger, _fetchUrlStorageService);
        var result = await service.CreateAsync(items, queryProviderInput, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.All(
            result,
            url =>
            {
                Assert.DoesNotContain(originalUrl, url.RecordUrl);
                var guid = url.RecordUrl.Split("/").Last();
                Assert.True(Guid.TryParseExact(guid, "N", out _));
            }
        );
    }

    [Fact]
    public async Task ReturnsUniqueMaskedUrls_ForDuplicateInputUrls()
    {
        // Arrange
        var originalUrl = "https://localhost.example.com/somewhere";
        List<CustodianSearchResultItem> items =
        [
            new("test-custodian", "Health", originalUrl, "bib", "bob"),
            new("test-custodian", "Education", originalUrl, "ll", "TestRecord"),
        ];
        var providerDefinition = new ProviderDefinition();
        var queryProviderInput = new QueryProviderInput(
            "requestingOrg",
            "job-id",
            "correlationId",
            "suid",
            providerDefinition
        );
        var service = new MaskUrlService(_logger, _fetchUrlStorageService);

        // Act
        var result = await service.CreateAsync(items, queryProviderInput, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(result.Select(x => x.RecordUrl).Distinct().Count(), result.Count);
    }
}
