using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using NSubstitute;
using OneOf.Types;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.ApplicationTests.Services.SearchServiceTests;

public class StartSearchAsyncTests : BaseSearchServiceTests
{
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");

    private const string ClientId = "test-client-id";
    private readonly string[] _scopes = [];
    private const string CorrelationId = "corr-id";
    private readonly CancellationToken _cancellationToken = CancellationToken.None;
    private readonly EncryptedPersonId _personId = new("encrypted-id");

    [Fact]
    public async Task ShouldReturnExistingJob_WhenDuplicateRequest()
    {
        var instanceId = $"{_personId}-{ClientId}";
        HashService.HmacSha256Hash(instanceId).Returns("hashed-id");

        var orchestrationMeta = new OrchestrationMetadata("SearchOrchestrator", "hashed-id")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
        };
        _client
            .GetInstanceAsync("hashed-id", Arg.Any<CancellationToken>())
            .Returns(orchestrationMeta);

        var result = await Sut.StartSearchAsync(
            _personId,
            ClientId,
            _scopes,
            _client,
            CorrelationId,
            _cancellationToken
        );

        Assert.IsType<SearchJobDto>(result.Value);
        var dto = result.AsT0;
        Assert.Equal("hashed-id", dto.JobId);
        Assert.Equal(_personId.EncryptedValue, dto.PersonId);
        Assert.Equal(SearchStatus.Running, dto.Status);
    }

    [Fact]
    public async Task ShouldReturnFailed_WhenCustodianNotFound()
    {
        var instanceId = $"{_personId}-{ClientId}";
        HashService.HmacSha256Hash(instanceId).Returns("hashed-id");
        _client
            .GetInstanceAsync("hashed-id", Arg.Any<CancellationToken>())
            .Returns((OrchestrationMetadata?)null);

        CustodianService
            .GetCustodianAsync(ClientId)
            .Returns(Domain.Models.Result<ProviderDefinition>.Fail("Custodian not found"));

        var result = await Sut.StartSearchAsync(
            _personId,
            ClientId,
            _scopes,
            _client,
            CorrelationId,
            _cancellationToken
        );

        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnFailed_WhenCustodianHasNoEncryption()
    {
        var instanceId = $"{_personId}-{ClientId}";
        HashService.HmacSha256Hash(instanceId).Returns("hashed-id");
        _client
            .GetInstanceAsync("hashed-id", Arg.Any<CancellationToken>())
            .Returns((OrchestrationMetadata?)null);

        var custodianDef = new ProviderDefinition { Encryption = null };
        CustodianService
            .GetCustodianAsync(ClientId)
            .Returns(
                Domain.Models.Result<ProviderDefinition>.Ok(
                    new ProviderDefinition() { Encryption = null }
                )
            );

        var result = await Sut.StartSearchAsync(
            _personId,
            ClientId,
            _scopes,
            _client,
            CorrelationId,
            _cancellationToken
        );

        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnFailed_WhenDecryptionFails()
    {
        var instanceId = $"{_personId}-{ClientId}";
        HashService.HmacSha256Hash(instanceId).Returns("hashed-id");
        _client
            .GetInstanceAsync("hashed-id", Arg.Any<CancellationToken>())
            .Returns((OrchestrationMetadata?)null);

        var custodianDef = new ProviderDefinition { Encryption = new EncryptionDefinition() };
        CustodianService
            .GetCustodianAsync(ClientId)
            .Returns(Domain.Models.Result<ProviderDefinition>.Ok(custodianDef));

        EncryptionService
            .DecryptPersonIdToNhs(_personId.EncryptedValue, custodianDef.Encryption)
            .Returns(Domain.Models.Result<string>.Fail("Decryption failed"));

        var result = await Sut.StartSearchAsync(
            _personId,
            ClientId,
            _scopes,
            _client,
            CorrelationId,
            _cancellationToken
        );

        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnSuccess_WhenNewJobScheduled()
    {
        var instanceId = $"{_personId}-{ClientId}";
        HashService.HmacSha256Hash(instanceId).Returns("hashed-id");
        _client
            .GetInstanceAsync("hashed-id", Arg.Any<CancellationToken>())
            .Returns((OrchestrationMetadata?)null);

        var custodianDef = new ProviderDefinition() { Encryption = new EncryptionDefinition() };
        CustodianService
            .GetCustodianAsync(ClientId)
            .Returns(Domain.Models.Result<ProviderDefinition>.Ok(custodianDef));

        EncryptionService
            .DecryptPersonIdToNhs(_personId.EncryptedValue, custodianDef.Encryption)
            .Returns(Domain.Models.Result<string>.Ok("decrypted-nhs-id"));

        _client
            .ScheduleNewOrchestrationInstanceAsync(
                "SearchOrchestrator",
                Arg.Any<SearchOrchestratorInput>(),
                Arg.Any<StartOrchestrationOptions>(),
                _cancellationToken
            )
            .Returns("job-123");

        var result = await Sut.StartSearchAsync(
            _personId,
            ClientId,
            _scopes,
            _client,
            CorrelationId,
            _cancellationToken
        );

        var dto = Assert.IsType<SearchJobDto>(result.Value);
        Assert.Equal("job-123", dto.JobId);
        Assert.Equal(_personId.EncryptedValue, dto.PersonId);
        Assert.Equal(SearchStatus.Queued, dto.Status);
    }
}
