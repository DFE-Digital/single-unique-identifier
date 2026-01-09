using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
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
    private readonly IAuditService _mockAuditService = Substitute.For<IAuditService>();
    private readonly FetchRecordService _sut;

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
        var resolvedMapping = new ResolvedFetchMapping(
            "http://target.url",
            "TargetOrg",
            "requesting-org",
            "record-type"
        );
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resolvedMapping);

        var providerDef = new ProviderDefinition
        {
            OrgId = "TargetOrg",
            Connection = new ConnectionDefinition { Auth = new AuthDefinition() },
        };
        _mockCustodianService
            .GetCustodianAsync("TargetOrg")
            .Returns(Domain.Models.Result<ProviderDefinition>.Ok(providerDef));
        _mockOutboundAuthService
            .GetAccessTokenAsync(providerDef, Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Ok("access-token"));

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
            .GetAsync("http://target.url", Arg.Any<string?>(), Arg.Any<CancellationToken>())
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
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Error());

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnError_WhenCustodianNotFound()
    {
        // Arrange
        var resolvedMapping = new ResolvedFetchMapping(
            "http://target.url",
            "TargetOrg",
            "requesting-org",
            "record-type"
        );
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resolvedMapping);

        _mockCustodianService
            .GetCustodianAsync("TargetOrg")
            .Returns(Domain.Models.Result<ProviderDefinition>.Fail("Custodian not found"));

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnError_WhenProviderClientFails()
    {
        // Arrange
        var resolvedMapping = new ResolvedFetchMapping(
            "http://target.url",
            "TargetOrg",
            "requesting-org",
            "record-type"
        );
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resolvedMapping);

        var providerDef = new ProviderDefinition
        {
            OrgId = "TargetOrg",
            Connection = new ConnectionDefinition { Auth = new AuthDefinition() },
        };
        _mockCustodianService
            .GetCustodianAsync("TargetOrg")
            .Returns(Domain.Models.Result<ProviderDefinition>.Ok(providerDef));
        _mockOutboundAuthService
            .GetAccessTokenAsync(providerDef, Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Ok("access-token"));

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
        var resolvedMapping = new ResolvedFetchMapping(
            "http://target.url",
            "TargetOrg",
            "requesting-org",
            "record-type"
        );
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resolvedMapping);

        var providerDef = new ProviderDefinition { OrgId = "TargetOrg" };
        _mockCustodianService
            .GetCustodianAsync("TargetOrg")
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
        var resolvedMapping = new ResolvedFetchMapping(
            "http://target.url",
            "TargetOrg",
            "requesting-org",
            "record-type"
        );
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resolvedMapping);

        var providerDef = new ProviderDefinition { OrgId = "TargetOrg" };
        _mockCustodianService
            .GetCustodianAsync("TargetOrg")
            .Returns(Domain.Models.Result<ProviderDefinition>.Ok(providerDef));

        _mockOutboundAuthService
            .GetAccessTokenAsync(providerDef, Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Ok("valid-token"));

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
        var resolvedMapping = new ResolvedFetchMapping(
            "http://target.url",
            "TargetOrg",
            "requesting-org",
            "record-type"
        );
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resolvedMapping);

        var providerDef = new ProviderDefinition { OrgId = "TargetOrg" };
        _mockCustodianService
            .GetCustodianAsync("TargetOrg")
            .Returns(Domain.Models.Result<ProviderDefinition>.Ok(providerDef));

        _mockOutboundAuthService
            .GetAccessTokenAsync(providerDef, Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Ok("valid-token"));

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
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new NotFound());

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnNotFound_WhenRecordIsNotFound()
    {
        // Arrange
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new NotFound());

        // Act
        var result = await _sut.FetchRecordAsync("fetch-id", "org-id", CancellationToken.None);

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task ShouldLogSuccessfulAudit_WhenRecordIsFound()
    {
        // Arrange
        var recordUrl = "http://target.url";
        var resolvedMapping = new ResolvedFetchMapping(
            recordUrl,
            "TargetOrg",
            "requesting-org",
            "record-type"
        );
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resolvedMapping);

        var providerDef = new ProviderDefinition
        {
            OrgId = "TargetOrg",
            Connection = new ConnectionDefinition { Auth = new AuthDefinition() },
        };
        _mockCustodianService
            .GetCustodianAsync("TargetOrg")
            .Returns(Domain.Models.Result<ProviderDefinition>.Ok(providerDef));
        _mockOutboundAuthService
            .GetAccessTokenAsync(providerDef, Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Ok("access-token"));

        var expectedResult = new CustodianRecord
        {
            Payload = JsonSerializer.Deserialize<JsonElement>("{}"),
        };

        var jsonResponse = JsonSerializer.Serialize(expectedResult);

        _mockProviderClient
            .GetAsync(recordUrl, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Ok(jsonResponse));

        var requestingOrgId = "org-id";

        // Act
        await _sut.FetchRecordAsync("fetch-id", requestingOrgId, CancellationToken.None);

        // Assert
        await _mockAuditService
            .Received(1)
            .WriteAccessAuditLogAsync(
                Arg.Is<AuditEvent>(ae =>
                    ValidateAuditEvent(ae, requestingOrgId, FetchOutcome.Success, true, recordUrl)
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ShouldLogAuditWithPolicyDenial_WhenPEPDeniesAccess()
    {
        // Arrange
        var recordUrl = "http://target.url";
        var resolvedMapping = new ResolvedFetchMapping(
            recordUrl,
            "TargetOrg",
            "requesting-org",
            "record-type"
        );
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resolvedMapping);

        var providerDef = new ProviderDefinition
        {
            OrgId = "TargetOrg",
            Connection = new ConnectionDefinition { Auth = new AuthDefinition() },
        };
        _mockCustodianService
            .GetCustodianAsync("TargetOrg")
            .Returns(Domain.Models.Result<ProviderDefinition>.Ok(providerDef));

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

        var requestingOrgId = "org-id";

        // Act
        await _sut.FetchRecordAsync("fetch-id", requestingOrgId, CancellationToken.None);

        // Assert
        await _mockAuditService
            .Received(1)
            .WriteAccessAuditLogAsync(
                Arg.Is<AuditEvent>(ae =>
                    ValidateAuditEvent(
                        ae,
                        requestingOrgId,
                        FetchOutcome.PolicyDenial,
                        true,
                        recordUrl
                    )
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ShouldLogAuditWithJobNotFound_WhenMappingResolutionFails()
    {
        // Arrange
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new NotFound());

        var requestingOrgId = "org-id";

        // Act
        await _sut.FetchRecordAsync("fetch-id", requestingOrgId, CancellationToken.None);

        // Assert
        await _mockAuditService
            .Received(1)
            .WriteAccessAuditLogAsync(
                Arg.Is<AuditEvent>(ae =>
                    ValidateAuditEvent(ae, requestingOrgId, FetchOutcome.JobNotFound, false, null)
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ShouldLogAuditWithAuthorizationFailure_WhenUnauthorized()
    {
        // Arrange
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Unauthorized());

        var requestingOrgId = "org-id";

        // Act
        await _sut.FetchRecordAsync("fetch-id", requestingOrgId, CancellationToken.None);

        // Assert
        await _mockAuditService
            .Received(1)
            .WriteAccessAuditLogAsync(
                Arg.Is<AuditEvent>(ae =>
                    ValidateAuditEvent(
                        ae,
                        requestingOrgId,
                        FetchOutcome.AuthorizationFailure,
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
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Error());

        var requestingOrgId = "org-id";

        // Act
        await _sut.FetchRecordAsync("fetch-id", requestingOrgId, CancellationToken.None);

        // Assert
        await _mockAuditService
            .Received(1)
            .WriteAccessAuditLogAsync(
                Arg.Is<AuditEvent>(ae =>
                    ValidateAuditEvent(ae, requestingOrgId, FetchOutcome.NetworkError, false, null)
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ShouldLogAuditWithRecordNotFound_WhenProviderReturnsFailure()
    {
        // Arrange
        var recordUrl = "http://target.url";
        var resolvedMapping = new ResolvedFetchMapping(
            recordUrl,
            "TargetOrg",
            "requesting-org",
            "record-type"
        );
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resolvedMapping);

        var providerDef = new ProviderDefinition
        {
            OrgId = "TargetOrg",
            Connection = new ConnectionDefinition { Auth = new AuthDefinition() },
        };
        _mockCustodianService
            .GetCustodianAsync("TargetOrg")
            .Returns(Domain.Models.Result<ProviderDefinition>.Ok(providerDef));
        _mockOutboundAuthService
            .GetAccessTokenAsync(providerDef, Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Ok("access-token"));

        _mockProviderClient
            .GetAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Domain.Models.Result<string>.Fail("Provider failure"));

        var requestingOrgId = "org-id";

        // Act
        await _sut.FetchRecordAsync("fetch-id", requestingOrgId, CancellationToken.None);

        // Assert
        await _mockAuditService
            .Received(1)
            .WriteAccessAuditLogAsync(
                Arg.Is<AuditEvent>(ae =>
                    ValidateAuditEvent(
                        ae,
                        requestingOrgId,
                        FetchOutcome.RecordNotFound,
                        true,
                        recordUrl
                    )
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ShouldAlwaysLogAudit_RegardlessOfOutcome()
    {
        // Arrange - setup for a network error scenario
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Error());

        var requestingOrgId = "org-id";

        // Act
        await _sut.FetchRecordAsync("fetch-id", requestingOrgId, CancellationToken.None);

        // Assert - verify audit was called exactly once
        await _mockAuditService
            .Received(1)
            .WriteAccessAuditLogAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldIncludeTimestamps_InAuditPayload()
    {
        // Arrange
        _mockMaskUrlService
            .ResolveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new NotFound());

        var requestingOrgId = "org-id";

        // Act
        await _sut.FetchRecordAsync("fetch-id", requestingOrgId, CancellationToken.None);

        // Assert
        await _mockAuditService
            .Received(1)
            .WriteAccessAuditLogAsync(
                Arg.Is<AuditEvent>(ae => ValidateTimestamps(ae)),
                Arg.Any<CancellationToken>()
            );
    }

    private static bool ValidateAuditEvent(
        AuditEvent auditEvent,
        string expectedRequestingOrgId,
        FetchOutcome expectedOutcome,
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
        if (payload.FetchOutcome != expectedOutcome)
            return false;

        // Verify record detail presence
        if (hasRecordDetail && payload.Record is null)
            return false;

        if (!hasRecordDetail && payload.Record is not null)
            return false;

        // If record URL is specified, verify it
        if (expectedRecordUrl is not null && payload.Record?.RecordUrl != expectedRecordUrl)
            return false;

        return true;
    }

    private static bool ValidateTimestamps(AuditEvent auditEvent)
    {
        // Verify timestamp is set in AuditEvent
        return auditEvent.Timestamp != default;
    }
}
