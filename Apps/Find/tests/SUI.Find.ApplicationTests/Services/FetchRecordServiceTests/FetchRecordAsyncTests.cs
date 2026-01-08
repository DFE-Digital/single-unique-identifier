using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OneOf.Types;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Application.Services;

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
    private readonly FetchRecordService _sut;

    public FetchRecordAsyncTests()
    {
        _sut = new FetchRecordService(
            _mockLogger,
            _mockMaskUrlService,
            _mockCustodianService,
            _mockProviderClient,
            _mockOutboundAuthService,
            _mockPolicyEnforcementService
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
    public async Task FetchRecordAsync_ReturnsOk_WhenSuccessful()
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
    public async Task FetchRecordAsync_ReturnsFail_WhenUrlServiceFails()
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
    public async Task FetchRecordAsync_ReturnsFail_WhenCustodianNotFound()
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
    public async Task FetchRecordAsync_ReturnsFail_WhenProviderClientFails()
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
    public async Task FetchRecordAsync_ReturnsFail_WhenNoAccessToken()
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
    public async Task FetchRecordAsync_ReturnsFail_WhenProviderReturnsEmptyContent()
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
    public async Task FetchRecordAsync_ReturnsFail_WhenDeserialisationResultIsNull()
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
    public async Task FetchRecordAsync_ReturnsUnauthorized_WhenRecordsDoNotBelongToCaller()
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
    public async Task FetchRecordAsync_ReturnsNotFound_WhenRecordsAreExpired()
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
    public async Task FetchRecordAsync_ReturnsNotFound_WhenRecordIsNotFound()
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
}
