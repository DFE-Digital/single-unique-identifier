using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using NSubstitute;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.ApplicationTests.Services.SearchServiceTests;

public class StartSearchAsyncTests : BaseSearchService
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

        Assert.IsType<SearchJobResult.Success>(result);
        var success = (SearchJobResult.Success)result;
        Assert.Equal("hashed-id", success.Job.JobId);
        Assert.Equal(_personId.EncryptedValue, success.Job.PersonId);
        Assert.Equal(SearchStatus.Running, success.Job.Status);
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
            .Returns(Result<ProviderDefinition>.Fail("Custodian not found"));

        var result = await Sut.StartSearchAsync(
            _personId,
            ClientId,
            _scopes,
            _client,
            CorrelationId,
            _cancellationToken
        );

        Assert.IsType<SearchJobResult.Failed>(result);
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
            .Returns(Result<ProviderDefinition>.Ok(new ProviderDefinition() { Encryption = null }));

        var result = await Sut.StartSearchAsync(
            _personId,
            ClientId,
            _scopes,
            _client,
            CorrelationId,
            _cancellationToken
        );

        Assert.IsType<SearchJobResult.Failed>(result);
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
            .Returns(Result<ProviderDefinition>.Ok(custodianDef));

        EncryptionService
            .DecryptPersonIdToNhs(_personId.EncryptedValue, custodianDef.Encryption)
            .Returns(Result<string>.Fail("Decryption failed"));

        var result = await Sut.StartSearchAsync(
            _personId,
            ClientId,
            _scopes,
            _client,
            CorrelationId,
            _cancellationToken
        );

        Assert.IsType<SearchJobResult.Failed>(result);
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
            .Returns(Result<ProviderDefinition>.Ok(custodianDef));

        EncryptionService
            .DecryptPersonIdToNhs(_personId.EncryptedValue, custodianDef.Encryption)
            .Returns(Result<string>.Ok("decrypted-nhs-id"));

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

        Assert.IsType<SearchJobResult.Success>(result);
        var success = (SearchJobResult.Success)result;
        Assert.Equal("job-123", success.Job.JobId);
        Assert.Equal(_personId.EncryptedValue, success.Job.PersonId);
        Assert.Equal(SearchStatus.Queued, success.Job.Status);
    }
}
