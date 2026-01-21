using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.AuditPayloads;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Application.Services;
using SUI.Find.Domain.Events.Audit;

namespace SUI.Find.ApplicationTests.Services.FetchRecordServiceTests;

public class FetchRecordAsyncTests
{
    private readonly ILogger<FetchRecordService> _mockLogger = Substitute.For<
        ILogger<FetchRecordService>
    >();
    private readonly IMaskUrlService _mockMaskUrlService = Substitute.For<IMaskUrlService>();
    private readonly ICustodianService _mockCustodianService = Substitute.For<ICustodianService>();
    private readonly IProviderHttpClient _mockProviderClient =
        Substitute.For<IProviderHttpClient>();
    private readonly IOutboundAuthService _mockOutboundAuthService =
        Substitute.For<IOutboundAuthService>();
    private readonly IPolicyEnforcementService _mockPolicyEnforcementService =
        Substitute.For<IPolicyEnforcementService>();
    private readonly IAuditQueueClient _mockAuditService = Substitute.For<IAuditQueueClient>();
    private readonly FetchRecordService _sut;
    private AuditEvent? _capturedAuditEvent;

    private readonly ResolvedFetchMapping _mockResolvedMapping = new(
        "http://target.url",
        "TargetOrg",
        "requesting-org",
        "record-type"
    );

    public FetchRecordAsyncTests()
    {
        _sut = new FetchRecordService(
            _mockLogger,
            _mockMaskUrlService,
            _mockCustodianService,
            _mockProviderClient,
            _mockOutboundAuthService,
            _mockPolicyEnforcementService,
            _mockAuditService,
            TimeProvider.System
        );

        //  Mock custodian lookup to return a valid org for PEP checks
        _mockCustodianService
            .GetCustodianAsync(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var orgId = callInfo.Arg<string>();
                return Domain.Models.Result<ProviderDefinition>.Ok(
                    new ProviderDefinition { OrgId = orgId, OrgType = "LOCAL_AUTHORITY" }
                );
            });

        // Default: PEP allows access
        _mockPolicyEnforcementService
            .EvaluateAsync(
                Arg.Any<PolicyDecisionRequest>(),
                Arg.Any<DsaPolicyDefinition>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                Task.FromResult(
                    new PolicyDecisionResult { IsAllowed = true, Reason = "Allowed by test setup" }
                )
            );
    }

    [Fact]
    public async Task ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        ArrangeResolvedMapping();
        ArrangeProviderWithAuth(_mockResolvedMapping.TargetOrgId);

        var expectedResult = new CustodianRecord
        {
            RecordId = "record-123",
            RecordType = "record-type",
            DataType = "data-type",
            PersonId = "person-456",
            SchemaUri = "schema-uri",
            Payload = JsonSerializer.Deserialize<JsonElement>("{}"),
        };

        var jsonResponse = JsonSerializer.Serialize(expectedResult);

        _mockProviderClient
            .GetAsync(
                _mockResolvedMapping.TargetUrl,
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Domain.Models.Result<string>.Ok(jsonResponse));

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        var body = Assert.IsType<CustodianRecord>(result.Value);
        Assert.Equal(expectedResult.RecordId, body.RecordId);
        Assert.Equal(expectedResult.PersonId, body.PersonId);
    }

    [Fact]
    public async Task ShouldReturnError_WhenUrlServiceFails()
    {
        // Arrange
        ArrangeResolvedMapping(returnValue: new Error());

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnError_WhenCustodianNotFound()
    {
        // Arrange
        ArrangeResolvedMapping();

        _mockCustodianService
            .GetCustodianAsync(_mockResolvedMapping.TargetOrgId)
            .Returns(Domain.Models.Result<ProviderDefinition>.Fail("Custodian not found"));

        // Act
        var result = await _sut.FetchRecordAsync(
            "fetch-id",
            _mockResolvedMapping.RequestingOrgId,
            CancellationToken.None
        );

        // Assert
        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnError_WhenProviderClientFails()
    {
        // Arrange
        ArrangeResolvedMapping();
        ArrangeProviderWithAuth(_mockResolvedMapping.TargetOrgId);

        _mockProviderClient
            .GetAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Fail("Upstream error"));

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnError_WhenNoAccessToken()
    {
        // Arrange
        ArrangeResolvedMapping();

        var providerDef = new ProviderDefinition { OrgId = _mockResolvedMapping.TargetOrgId };
        _mockCustodianService
            .GetCustodianAsync(_mockResolvedMapping.TargetOrgId)
            .Returns(Domain.Models.Result<ProviderDefinition>.Ok(providerDef));

        _mockOutboundAuthService
            .GetAccessTokenAsync(providerDef, Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Fail("Auth Service Unavailable"));

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnError_WhenProviderReturnsEmptyContent()
    {
        // Arrange
        ArrangeResolvedMapping();
        ArrangeProviderWithAuth(_mockResolvedMapping.TargetOrgId);

        _mockProviderClient
            .GetAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Ok(string.Empty));

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnError_WhenDeserialisationResultIsNull()
    {
        // Arrange
        ArrangeResolvedMapping();
        ArrangeProviderWithAuth(_mockResolvedMapping.TargetOrgId);

        _mockProviderClient
            .GetAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Ok("null"));

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnUnauthorized_WhenRecordsDoNotBelongToCaller()
    {
        // Arrange
        ArrangeResolvedMapping();
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Unauthorized());

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.IsType<Unauthorized>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnNotFound_WhenRecordsAreExpired()
    {
        // Arrange
        ArrangeResolvedMapping(returnValue: new NotFound());

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnNotFound_WhenRecordIsNotFound()
    {
        // Arrange
        ArrangeResolvedMapping(returnValue: new NotFound());

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task ShouldLogSuccessfulAudit_WhenRecordIsFound()
    {
        // Arrange
        ArrangeResolvedMapping();
        ArrangeProviderWithAuth(_mockResolvedMapping.TargetOrgId);

        var expectedResult = new CustodianRecord
        {
            Payload = JsonSerializer.Deserialize<JsonElement>("{}"),
        };

        var jsonResponse = JsonSerializer.Serialize(expectedResult);

        _mockProviderClient
            .GetAsync(
                _mockResolvedMapping.TargetUrl,
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Domain.Models.Result<string>.Ok(jsonResponse));

        // Act
        await _sut.FetchRecordAsync(
            "fetch-id",
            _mockResolvedMapping.RequestingOrgId,
            CancellationToken.None
        );

        // Assert
        await _mockAuditService
            .Received(1)
            .SendMessageAsync(
                Arg.Is<AuditEvent>(ae =>
                    ValidateAuditEvent(
                        ae,
                        _mockResolvedMapping.RequestingOrgId,
                        RequestStatus.Success,
                        true,
                        _mockResolvedMapping.TargetUrl
                    )
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ShouldLogAuditWithPolicyDenial_WhenPEPDeniesAccess()
    {
        // Arrange
        ArrangeResolvedMapping();

        ArrangeProviderWithAuth(_mockResolvedMapping.TargetOrgId);

        _mockPolicyEnforcementService
            .EvaluateAsync(
                Arg.Any<PolicyDecisionRequest>(),
                Arg.Any<DsaPolicyDefinition>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                Task.FromResult(
                    new PolicyDecisionResult { IsAllowed = false, Reason = "Policy denied" }
                )
            );

        // Act
        await _sut.FetchRecordAsync(
            "fetch-id",
            _mockResolvedMapping.RequestingOrgId,
            CancellationToken.None
        );

        // Assert
        await _mockAuditService
            .Received(1)
            .SendMessageAsync(
                Arg.Is<AuditEvent>(ae =>
                    ValidateAuditEvent(
                        ae,
                        _mockResolvedMapping.RequestingOrgId,
                        RequestStatus.Failure,
                        true,
                        _mockResolvedMapping.TargetUrl
                    )
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ShouldLogAuditWithJobNotFound_WhenMappingResolutionFails()
    {
        // Arrange
        ArrangeResolvedMapping(returnValue: new NotFound());

        // Act
        await _sut.FetchRecordAsync(
            "fetch-id",
            _mockResolvedMapping.RequestingOrgId,
            CancellationToken.None
        );

        // Assert
        await _mockAuditService
            .Received(1)
            .SendMessageAsync(
                Arg.Is<AuditEvent>(ae =>
                    ValidateAuditEvent(
                        ae,
                        _mockResolvedMapping.RequestingOrgId,
                        RequestStatus.Failure,
                        false,
                        null
                    )
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ShouldLogAuditWithAuthorizationFailure_WhenUnauthorized()
    {
        // Arrange
        ArrangeResolvedMapping(returnValue: new Unauthorized());

        // Act
        await _sut.FetchRecordAsync(
            "fetch-id",
            _mockResolvedMapping.RequestingOrgId,
            CancellationToken.None
        );

        // Assert
        await _mockAuditService
            .Received(1)
            .SendMessageAsync(
                Arg.Is<AuditEvent>(ae =>
                    ValidateAuditEvent(
                        ae,
                        _mockResolvedMapping.RequestingOrgId,
                        RequestStatus.Failure,
                        false,
                        null
                    )
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ShouldLogAuditWithNetworkError_WhenMaskUrlServiceFails()
    {
        // Arrange
        ArrangeResolvedMapping(returnValue: new Error());

        // Act
        await _sut.FetchRecordAsync(
            "fetch-id",
            _mockResolvedMapping.RequestingOrgId,
            CancellationToken.None
        );

        // Assert
        await _mockAuditService
            .Received(1)
            .SendMessageAsync(
                Arg.Is<AuditEvent>(ae =>
                    ValidateAuditEvent(
                        ae,
                        _mockResolvedMapping.RequestingOrgId,
                        RequestStatus.Failure,
                        false,
                        null
                    )
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ShouldLogAuditWithRecordNotFound_WhenProviderReturnsFailure()
    {
        // Arrange
        ArrangeResolvedMapping();

        ArrangeProviderWithAuth(_mockResolvedMapping.TargetOrgId);

        _mockProviderClient
            .GetAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Fail("Provider failure"));

        // Act
        await _sut.FetchRecordAsync(
            "fetch-id",
            _mockResolvedMapping.RequestingOrgId,
            CancellationToken.None
        );

        // Assert
        await _mockAuditService
            .Received(1)
            .SendMessageAsync(
                Arg.Is<AuditEvent>(ae =>
                    ValidateAuditEvent(
                        ae,
                        _mockResolvedMapping.RequestingOrgId,
                        RequestStatus.Failure,
                        true,
                        _mockResolvedMapping.TargetUrl
                    )
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ShouldAlwaysLogAudit_RegardlessOfOutcome()
    {
        // Arrange - setup for a network error scenario
        ArrangeResolvedMapping(returnValue: new Error());

        // Act
        await _sut.FetchRecordAsync(
            "fetch-id",
            _mockResolvedMapping.RequestingOrgId,
            CancellationToken.None
        );

        // Assert - verify audit was called exactly once
        await _mockAuditService
            .Received(1)
            .SendMessageAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldIncludeTimestamps_InAuditPayload()
    {
        // Arrange
        ArrangeResolvedMapping();
        ArrangeProviderWithAuth(_mockResolvedMapping.TargetOrgId);
        ArrangeProviderResponse(_mockResolvedMapping.TargetUrl, "{}");

        // Act
        await _sut.FetchRecordAsync(
            "fetch-id",
            _mockResolvedMapping.RequestingOrgId,
            CancellationToken.None
        );

        // Assert
        await _mockAuditService
            .Received(1)
            .SendMessageAsync(
                Arg.Is<AuditEvent>(ae => ValidateTimestamps(ae)),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ShouldLogAuditWithUndeterministic_WhenCustodianCannotBeRead()
    {
        // Arrange
        ArrangeResolvedMapping();

        // Simulate custodian read failure
        _mockCustodianService
            .GetCustodianAsync("TargetOrg")
            .Returns(Domain.Models.Result<ProviderDefinition>.Fail("Custodian read error"));

        // Act
        await _sut.FetchRecordAsync(
            "fetch-id",
            _mockResolvedMapping.RequestingOrgId,
            CancellationToken.None
        );

        // Assert
        await _mockAuditService
            .Received(1)
            .SendMessageAsync(
                Arg.Is<AuditEvent>(ae =>
                    ValidateUndeterministic(ae, _mockResolvedMapping.RequestingOrgId)
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ShouldLogAudit_WithReceivedByteCount_WhenRecordIsFetched()
    {
        // Arrange
        ArrangeResolvedMapping();
        ArrangeProviderWithAuth(_mockResolvedMapping.TargetOrgId);

        var expectedResult = new CustodianRecord
        {
            RecordId = "record-123",
            RecordType = "record-type",
            DataType = "data-type",
            PersonId = "person-456",
            SchemaUri = "schema-uri",
            Payload = JsonSerializer.Deserialize<JsonElement>("{\"foo\":\"bar\"}"),
        };
        var jsonResponse = JsonSerializer.Serialize(expectedResult, JsonSerializerOptions.Web);
        ArrangeProviderResponse(_mockResolvedMapping.TargetUrl, jsonResponse);

        StartCapturingAuditEvent();

        // Act
        await _sut.FetchRecordAsync(
            "fetch-id",
            _mockResolvedMapping.RequestingOrgId,
            CancellationToken.None
        );

        // Assert
        await _mockAuditService
            .Received(1)
            .SendMessageAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>());

        Assert.NotNull(_capturedAuditEvent);
        var payload = JsonSerializer.Deserialize<PepFetchPayload>(
            _capturedAuditEvent!.Payload.GetRawText()
        );
        Assert.NotNull(payload);
        Assert.True(payload.ReceivedByteCount > 0);
    }

    [Fact]
    public async Task ShouldLogAudit_WithZeroReceivedByteCount_WhenResponseIsEmpty()
    {
        // Arrange
        ArrangeResolvedMapping();
        ArrangeProviderWithAuth(_mockResolvedMapping.TargetOrgId);

        // Provider returns empty body leading to RecordNotFound path
        ArrangeProviderResponse(_mockResolvedMapping.TargetUrl, string.Empty);

        StartCapturingAuditEvent();

        // Act
        await _sut.FetchRecordAsync(
            "fetch-id",
            _mockResolvedMapping.RequestingOrgId,
            CancellationToken.None
        );

        // Assert
        await _mockAuditService
            .Received(1)
            .SendMessageAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>());

        Assert.NotNull(_capturedAuditEvent);
        var payload = JsonSerializer.Deserialize<PepFetchPayload>(
            _capturedAuditEvent!.Payload.GetRawText()
        );
        Assert.NotNull(payload);
        Assert.Equal(0, payload.ReceivedByteCount);
    }

    private static bool ValidateUndeterministic(AuditEvent ae, string requestingOrgId)
    {
        var payload = JsonSerializer.Deserialize<PepFetchPayload>(ae.Payload.GetRawText());
        return payload is not null
            && ae.Actor.ActorId == requestingOrgId
            && payload.RequestStatus == RequestStatus.Failure
            && !string.IsNullOrEmpty(payload.RequestStatusMessage);
    }

    private static bool ValidateAuditEvent(
        AuditEvent auditEvent,
        string expectedRequestingOrgId,
        RequestStatus expectedOutcome,
        bool hasRecordDetail,
        string? expectedRecordUrl = null
    )
    {
        var payload = JsonSerializer.Deserialize<PepFetchPayload>(auditEvent.Payload.GetRawText());

        if (payload is null)
            return false;

        // Verify actor ID matches requesting org
        if (auditEvent.Actor.ActorId != expectedRequestingOrgId)
            return false;

        // Verify destination org ID in payload
        if (payload.DestinationOrgId != expectedRequestingOrgId)
            return false;

        // Verify fetch outcome
        if (payload.RequestStatus != expectedOutcome)
            return false;

        if (string.IsNullOrEmpty(payload.RequestStatusMessage))
            return false;

        // Verify record detail presence
        if (hasRecordDetail && payload.PolicySnapshot is null)
            return false;

        if (!hasRecordDetail && payload.PolicySnapshot is not null)
            return false;

        // If record URL is specified, verify it
        if (expectedRecordUrl is not null && payload.PolicySnapshot?.RecordUrl != expectedRecordUrl)
            return false;

        return true;
    }

    private static bool ValidateTimestamps(AuditEvent auditEvent)
    {
        // Verify timestamp is set in AuditEvent
        return auditEvent.Timestamp != default;
    }

    // Test helpers to reduce duplication in Arrange sections
    private void ArrangeResolvedMapping(
        string? recordUrl = null,
        string? targetOrgId = null,
        string? requestingOrgId = null,
        string? recordType = null,
        OneOf<ResolvedFetchMapping, NotFound, Unauthorized, Error>? returnValue = null
    )
    {
        var resolvedMapping = new ResolvedFetchMapping(
            recordUrl ?? _mockResolvedMapping.TargetUrl,
            targetOrgId ?? _mockResolvedMapping.TargetOrgId,
            requestingOrgId ?? _mockResolvedMapping.RequestingOrgId,
            recordType ?? _mockResolvedMapping.RecordType
        );
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(returnValue ?? resolvedMapping);
    }

    private void ArrangeProviderWithAuth(string targetOrgId)
    {
        var providerDef = new ProviderDefinition
        {
            OrgId = targetOrgId,
            Connection = new ConnectionDefinition { Auth = new AuthDefinition() },
        };
        _mockCustodianService
            .GetCustodianAsync(targetOrgId)
            .Returns(Domain.Models.Result<ProviderDefinition>.Ok(providerDef));
        _mockOutboundAuthService
            .GetAccessTokenAsync(providerDef, Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Ok("access-token"));
    }

    private void ArrangeProviderResponse(string recordUrl, string jsonResponse)
    {
        _mockProviderClient
            .GetAsync(recordUrl, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Ok(jsonResponse));
    }

    private void StartCapturingAuditEvent()
    {
        _capturedAuditEvent = null;
        _mockAuditService
            .When(x => x.SendMessageAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>()))
            .Do(ci => _capturedAuditEvent = ci.Arg<AuditEvent>());
    }
}
