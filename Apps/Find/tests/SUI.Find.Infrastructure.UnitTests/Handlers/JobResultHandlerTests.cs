using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Handlers;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Repositories.JobRepository;
using SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

namespace SUI.Find.Infrastructure.UnitTests.Handlers;

public class JobResultHandlerTests
{
    private readonly ILogger<JobResultHandler> _logger = Substitute.For<
        ILogger<JobResultHandler>
    >();
    private readonly IJobProcessorService _jobService = Substitute.For<IJobProcessorService>();
    private readonly IIdRegisterRepository _idRegisterRepo =
        Substitute.For<IIdRegisterRepository>();
    private readonly IWorkItemJobCountRepository _jobCountRepo =
        Substitute.For<IWorkItemJobCountRepository>();
    private readonly ICustodianService _custodianService = Substitute.For<ICustodianService>();
    private readonly IPolicyEnforcementService _pepService =
        Substitute.For<IPolicyEnforcementService>();
    private readonly ISearchResultEntryRepository _searchResultRepo =
        Substitute.For<ISearchResultEntryRepository>();

    private readonly JobResultHandler _handler;

    public JobResultHandlerTests()
    {
        _handler = new JobResultHandler(
            _logger,
            _jobService,
            _idRegisterRepo,
            _jobCountRepo,
            _custodianService,
            _pepService,
            _searchResultRepo
        );
    }

    private static JobResultMessage CreateMessage(
        JobType jobType = JobType.CustodianLookup,
        int recordCount = 1
    )
    {
        return new JobResultMessage
        {
            JobId = "job1",
            WorkItemId = "work1",
            CustodianId = "cust1",
            LeaseId = "lease1",
            JobType = jobType,
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            Records = Enumerable
                .Range(1, recordCount)
                .Select(i => new JobResultRecord
                {
                    RecordId = $"r{i}",
                    RecordType = "TypeA",
                    SystemId = "sys1",
                    RecordUrl = $"url{i}",
                })
                .ToList(),
        };
    }

    [Fact]
    public async Task HandleAsync_ShouldMarkCompleted_WhenJobTypeUnsupported()
    {
        var message = CreateMessage(jobType: JobType.Unknown);

        await _handler.HandleAsync(message, CancellationToken.None);

        await _jobService
            .Received(1)
            .MarkCompletedAsync(message.JobId, message.CustodianId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldMarkCompleted_WhenNoRecords()
    {
        var message = CreateMessage(recordCount: 0);

        await _handler.HandleAsync(message, CancellationToken.None);

        await _jobService
            .Received(1)
            .MarkCompletedAsync(message.JobId, message.CustodianId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturn_WhenPayloadNull()
    {
        var message = CreateMessage();

        _jobCountRepo
            .GetByWorkItemIdAndJobTypeAsync(
                message.WorkItemId,
                message.JobType,
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult<WorkItemJobCount?>(null));

        await _handler.HandleAsync(message, CancellationToken.None);

        await _jobService
            .DidNotReceive()
            .MarkCompletedAsync(message.JobId, message.CustodianId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturn_WhenCustodianOrSearchOrgMissing()
    {
        var message = CreateMessage();
        var payload = new SearchWorkItemPayload("sui1");
        _jobCountRepo
            .GetByWorkItemIdAndJobTypeAsync(
                message.WorkItemId,
                message.JobType,
                Arg.Any<CancellationToken>()
            )!
            .Returns(
                Task.FromResult(
                    new WorkItemJobCount
                    {
                        PayloadJson = JsonSerializer.Serialize(payload),
                        WorkItemId = message.WorkItemId,
                        JobType = message.JobType,
                        SearchingOrganisationId = message.CustodianId,
                    }
                )
            );

        _jobService
            .GetJobByIdAndCustodianIdAsync(
                message.JobId,
                message.CustodianId,
                Arg.Any<CancellationToken>()
            )
            .Returns(
                Task.FromResult<Job?>(
                    new Job
                    {
                        JobId = message.JobId,
                        SearchingOrganisationId = "orgX",
                        CustodianId = message.CustodianId,
                        JobType = JobType.Unknown,
                        PayloadJson = "{}",
                    }
                )
            );

        // Simulate missing custodian
        _custodianService
            .GetCustodianAsync(message.CustodianId)
            .Returns(Domain.Models.Result<ProviderDefinition>.Fail("Not found"));

        await _handler.HandleAsync(message, CancellationToken.None);

        await _searchResultRepo
            .DidNotReceive()
            .UpsertAsync(Arg.Any<SearchResultEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldUpsertRecords_WhenValid()
    {
        // ARRANGE
        var message = CreateMessage(recordCount: 2);
        var payload = new SearchWorkItemPayload("sui1");

        _jobCountRepo
            .GetByWorkItemIdAndJobTypeAsync(
                message.WorkItemId,
                message.JobType,
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new WorkItemJobCount
                {
                    PayloadJson = JsonSerializer.Serialize(payload),
                    WorkItemId = message.WorkItemId,
                    JobType = message.JobType,
                    SearchingOrganisationId = message.CustodianId,
                }
            );

        _jobService
            .GetJobByIdAndCustodianIdAsync(
                message.JobId,
                message.CustodianId,
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new Job
                {
                    JobId = message.JobId,
                    SearchingOrganisationId = "orgX",
                    CustodianId = message.CustodianId,
                    JobType = JobType.Unknown,
                    PayloadJson = "{}",
                }
            );

        var provider = new ProviderDefinition
        {
            OrgId = "orgX",
            OrgName = "OrgX",
            OrgType = "TypeA",
            Encryption = new EncryptionDefinition { Key = "test-key" },
            DsaPolicy = new DsaPolicyDefinition(),
        };

        _custodianService
            .GetCustodianAsync(Arg.Any<string>())
            .Returns(Domain.Models.Result<ProviderDefinition>.Ok(provider));

        // Proper PEP mock (based on actual inputs)
        _pepService
            .FilterResultsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<CustodianSearchResultItem>>(),
                Arg.Any<DsaPolicyDefinition>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(callInfo =>
            {
                var sourceOrgId = callInfo.ArgAt<string>(0);
                var destOrgId = callInfo.ArgAt<string>(1);
                var items = callInfo.ArgAt<IReadOnlyList<CustodianSearchResultItem>>(3);

                return items
                    .Select(item => new SearchResultWithDecision(
                        item,
                        sourceOrgId,
                        destOrgId,
                        new PolicyDecisionResult { IsAllowed = true, Reason = "Mocked-Allow" }
                    ))
                    .ToList();
            });

        // ACT
        await _handler.HandleAsync(message, CancellationToken.None);

        // ASSERT

        // Correct Provenance
        await _idRegisterRepo
            .Received(2)
            .UpsertAsync(
                Arg.Is<IdRegisterEntry>(x =>
                    x.Sui == payload.Sui
                    && x.CustodianId == message.CustodianId
                    && x.Provenance == Provenance.AlreadyHeldByCustodian
                ),
                Arg.Any<CancellationToken>()
            );

        // Search results persisted (only allowed ones)
        await _searchResultRepo
            .Received(2)
            .UpsertAsync(
                Arg.Is<SearchResultEntry>(x =>
                    x.JobId == message.JobId
                    && x.WorkItemId == message.WorkItemId
                    && x.CustodianId == message.CustodianId
                ),
                Arg.Any<CancellationToken>()
            );

        // Job completion
        await _jobService
            .Received(1)
            .MarkCompletedAsync(message.JobId, message.CustodianId, Arg.Any<CancellationToken>());

        // Verify PEP interaction
        await _pepService
            .Received(1)
            .FilterResultsAsync(
                "orgX", // source (custodian)
                "orgX", // destination (searching org)
                "TypeA",
                Arg.Is<IReadOnlyList<CustodianSearchResultItem>>(x => x.Count == 2),
                Arg.Any<DsaPolicyDefinition>(),
                ApplicationConstants.PolicyEnforcementPurposes.Safeguarding,
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleAsync_ShouldPersistOnlyAllowedResults_WhenPepFiltersSomeOut()
    {
        // ARRANGE
        var message = CreateMessage(recordCount: 3);
        var payload = new SearchWorkItemPayload("sui1");

        _jobCountRepo
            .GetByWorkItemIdAndJobTypeAsync(
                message.WorkItemId,
                message.JobType,
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new WorkItemJobCount
                {
                    PayloadJson = JsonSerializer.Serialize(payload),
                    WorkItemId = message.WorkItemId,
                    JobType = message.JobType,
                    SearchingOrganisationId = message.CustodianId,
                }
            );

        _jobService
            .GetJobByIdAndCustodianIdAsync(
                message.JobId,
                message.CustodianId,
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new Job
                {
                    JobId = message.JobId,
                    SearchingOrganisationId = "orgX",
                    CustodianId = message.CustodianId,
                    JobType = JobType.Unknown,
                    PayloadJson = "{}",
                }
            );

        var provider = new ProviderDefinition
        {
            OrgId = "orgX",
            OrgName = "OrgX",
            OrgType = "TypeA",
            Encryption = new EncryptionDefinition { Key = "test-key" },
            DsaPolicy = new DsaPolicyDefinition(),
        };

        _custodianService
            .GetCustodianAsync(Arg.Any<string>())
            .Returns(Result<ProviderDefinition>.Ok(provider));

        // Mixed PEP response (allow only first 2)
        _pepService
            .FilterResultsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<CustodianSearchResultItem>>(),
                Arg.Any<DsaPolicyDefinition>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(callInfo =>
            {
                var sourceOrgId = callInfo.ArgAt<string>(0);
                var destOrgId = callInfo.ArgAt<string>(1);
                var items = callInfo.ArgAt<IReadOnlyList<CustodianSearchResultItem>>(3);

                return items
                    .Select(
                        (item, index) =>
                            new SearchResultWithDecision(
                                item,
                                sourceOrgId,
                                destOrgId,
                                new PolicyDecisionResult
                                {
                                    IsAllowed = index < 2, // only first 2 allowed
                                    Reason = index < 2 ? "Allowed" : "Denied",
                                }
                            )
                    )
                    .ToList();
            });

        // ACT
        await _handler.HandleAsync(message, CancellationToken.None);

        // ASSERT

        // ID Register gets ALL records (3)
        await _idRegisterRepo
            .Received(3)
            .UpsertAsync(
                Arg.Is<IdRegisterEntry>(x => x.Sui == payload.Sui),
                Arg.Any<CancellationToken>()
            );

        // ONLY allowed results persisted (2)
        await _searchResultRepo
            .Received(2)
            .UpsertAsync(Arg.Any<SearchResultEntry>(), Arg.Any<CancellationToken>());

        // Ensure denied record NOT persisted
        await _searchResultRepo
            .DidNotReceive()
            .UpsertAsync(
                Arg.Is<SearchResultEntry>(x => x.RecordId == "r3"),
                Arg.Any<CancellationToken>()
            );

        // Job still completes
        await _jobService
            .Received(1)
            .MarkCompletedAsync(message.JobId, message.CustodianId, Arg.Any<CancellationToken>());
    }
}
