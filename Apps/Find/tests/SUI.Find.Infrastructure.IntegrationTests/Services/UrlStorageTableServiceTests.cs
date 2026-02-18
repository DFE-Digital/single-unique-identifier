using Microsoft.Extensions.Logging.Abstractions;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.IntegrationTests.Services;

public class UrlStorageTableServiceTests : IAsyncLifetime
{
    private readonly UrlStorageTableService _sut = new(
        NullLogger<UrlStorageTableService>.Instance,
        TableStorageFixture.Client
    );

    public async Task InitializeAsync() =>
        await _sut.EnsureTableExistsAsync(CancellationToken.None);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAsync_CreatesRecord_AsExpected()
    {
        // ARRANGE
        var request = new AddFetchUrlRequest
        {
            FetchId = $"Fetch_{Guid.NewGuid()}",
            TargetUrl = "https://example.com",
            TargetOrg = "TargetOrg",
            RequestingOrg = "ABCDE12345",
            RecordType = "SomeType",
            JobId = "Job123",
            Ttl = TimeSpan.FromMinutes(5),
        };

        var expectedPartitionKey = request.RequestingOrg[..5];

        // ACT
        await _sut.AddAsync(request, CancellationToken.None);

        // ASSERT
        var entity = (
            await TableStorageFixture
                .Client.GetTableClient(InfrastructureConstants.StorageTableUrlMappings.TableName)
                .GetEntityAsync<FetchUrlMappingEntity>(expectedPartitionKey, request.FetchId)
        ).Value;

        entity.TargetUrl.Should().Be(request.TargetUrl);
        entity.TargetOrgId.Should().Be(request.TargetOrg);
        entity.RequestingOrgId.Should().Be(request.RequestingOrg);
        entity.RecordType.Should().Be(request.RecordType);
        entity.JobId.Should().Be(request.JobId);
        entity.ExpiresAtUtc.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GetAsync_ReturnsResolvedFetchMapping_WhenValid()
    {
        // ARRANGE
        var requestingOrg = "ABCDE12345";
        var fetchId = $"Fetch_{Guid.NewGuid()}";

        await _sut.AddAsync(
            new AddFetchUrlRequest
            {
                FetchId = fetchId,
                TargetUrl = "https://example.com",
                TargetOrg = "TargetOrg",
                RequestingOrg = requestingOrg,
                RecordType = "SomeType",
                JobId = "Job123",
                Ttl = TimeSpan.FromMinutes(5),
            },
            CancellationToken.None
        );

        // ACT
        var result = await _sut.GetAsync(requestingOrg, fetchId, CancellationToken.None);

        // ASSERT
        result.IsT0.Should().BeTrue();
        result.AsT0.TargetUrl.Should().Be("https://example.com");
        result.AsT0.TargetOrgId.Should().Be("TargetOrg");
        result.AsT0.RequestingOrgId.Should().Be(requestingOrg);
        result.AsT0.RecordType.Should().Be("SomeType");
    }

    [Fact]
    public async Task GetAsync_ReturnsNotFound_WhenExpired()
    {
        // ARRANGE
        var requestingOrg = "ABCDE12345";
        var fetchId = $"Fetch_{Guid.NewGuid()}";
        var partitionKey = requestingOrg[..5];

        var tableClient = TableStorageFixture.Client.GetTableClient(
            InfrastructureConstants.StorageTableUrlMappings.TableName
        );

        await tableClient.AddEntityAsync(
            new FetchUrlMappingEntity
            {
                PartitionKey = partitionKey,
                RowKey = fetchId,
                TargetUrl = "https://example.com",
                TargetOrgId = "TargetOrg",
                RequestingOrgId = requestingOrg,
                RecordType = "SomeType",
                JobId = "Job123",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            }
        );

        // ACT
        var result = await _sut.GetAsync(requestingOrg, fetchId, CancellationToken.None);

        // ASSERT
        result.IsT1.Should().BeTrue(); // NotFound
    }

    [Fact]
    public async Task GetAsync_ReturnsUnauthorized_WhenRequestingOrgMismatch()
    {
        // ARRANGE
        var correctOrg = "ABCDE12345";
        var wrongOrg = "ABCDE99999"; // same first 5 chars, different full org
        var fetchId = $"Fetch_{Guid.NewGuid()}";

        await _sut.AddAsync(
            new AddFetchUrlRequest
            {
                FetchId = fetchId,
                TargetUrl = "https://example.com",
                TargetOrg = "TargetOrg",
                RequestingOrg = correctOrg,
                RecordType = "SomeType",
                JobId = "Job123",
                Ttl = TimeSpan.FromMinutes(5),
            },
            CancellationToken.None
        );

        // ACT
        var result = await _sut.GetAsync(wrongOrg, fetchId, CancellationToken.None);

        // ASSERT
        result.IsT2.Should().BeTrue(); // Unauthorized
    }

    [Fact]
    public async Task GetAsync_ReturnsError_WhenEntityMissing()
    {
        // ACT
        var result = await _sut.GetAsync(
            "ABCDE12345",
            Guid.NewGuid().ToString(),
            CancellationToken.None
        );

        // ASSERT
        result.IsT3.Should().BeTrue(); // Error
    }

    [Fact]
    public async Task AddAsync_UsesFirstFiveCharactersOfRequestingOrg_AsPartitionKey()
    {
        // ARRANGE
        var requestingOrg = "ABCDE123456789";
        var fetchId = $"Fetch_{Guid.NewGuid()}";

        var request = new AddFetchUrlRequest
        {
            FetchId = fetchId,
            TargetUrl = "https://example.com",
            TargetOrg = "TargetOrg",
            RequestingOrg = requestingOrg,
            RecordType = "SomeType",
            JobId = "Job123",
            Ttl = TimeSpan.FromMinutes(5),
        };

        var expectedPartitionKey = "ABCDE"; // explicitly assert slicing logic

        // ACT
        await _sut.AddAsync(request, CancellationToken.None);

        // ASSERT
        var tableClient = TableStorageFixture.Client.GetTableClient(
            InfrastructureConstants.StorageTableUrlMappings.TableName
        );

        var entity = (
            await tableClient.GetEntityAsync<FetchUrlMappingEntity>(expectedPartitionKey, fetchId)
        ).Value;

        entity.PartitionKey.Should().Be(expectedPartitionKey);
    }

    [Fact]
    public async Task AddAsync_Throws_WhenRequestingOrgTooShort()
    {
        // ARRANGE
        var request = new AddFetchUrlRequest
        {
            FetchId = $"Fetch_{Guid.NewGuid()}",
            TargetUrl = "https://example.com",
            TargetOrg = "TargetOrg",
            RequestingOrg = "ABC", // less than 5 chars
            RecordType = "SomeType",
            JobId = "Job123",
            Ttl = TimeSpan.FromMinutes(5),
        };

        // ACT
        Func<Task> act = async () => await _sut.AddAsync(request, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetAsync_ReturnsError_WhenRequestingOrgTooShort()
    {
        // ACT
        var result = await _sut.GetAsync(
            "ABC", // less than 5 chars
            Guid.NewGuid().ToString(),
            CancellationToken.None
        );

        // ASSERT
        result.IsT3.Should().BeTrue(); // Error
    }
}
