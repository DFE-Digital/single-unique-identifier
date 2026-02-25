using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OneOf;
using OneOf.Types;
using SUI.Find.Application.Configurations;
using SUI.Find.Application.Enums.Matching;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Interfaces.Matching;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Matching;
using SUI.Find.Application.Services;
using SUI.Find.Domain.Models;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.Application.UnitTests.Services.PersonIdRepresentationServiceTests;

public class FindPersonIdAsyncTests
{
    private readonly MatchPersonOrchestrationService _sut;
    private readonly IMatchingService _matchingService = Substitute.For<IMatchingService>();
    private readonly ICustodianService _custodianService = Substitute.For<ICustodianService>();
    private readonly IPersonIdEncryptionService _encryptionService =
        Substitute.For<IPersonIdEncryptionService>();
    private readonly IOptions<EncryptionConfiguration> _encryptionConfiguration = Substitute.For<
        IOptions<EncryptionConfiguration>
    >();

    public FindPersonIdAsyncTests()
    {
        var logger = Substitute.For<ILogger<MatchPersonOrchestrationService>>();

        _sut = new MatchPersonOrchestrationService(
            logger,
            _matchingService,
            _custodianService,
            _encryptionService,
            _encryptionConfiguration
        );
    }

    [Fact]
    public async Task ShouldReturnPlainPersonId_WhenGlobalEncryptionDisabled_AndEncryptionDoesNotKeyExist()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        _encryptionConfiguration.Value.Returns(
            new EncryptionConfiguration() { EnablePersonIdEncryption = false }
        );
        _custodianService
            .GetCustodianAsync("test-client-id")
            .Returns(
                Domain.Models.Result<ProviderDefinition>.Ok(
                    new ProviderDefinition()
                    {
                        Encryption = new EncryptionDefinition() { Key = null! },
                    }
                )
            );
        var nhsPersonId = NhsPersonId.Create("9999999999").Value;
        _matchingService
            .MatchPersonAsync(personSpec, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<OneOf<NhsPersonId, DataQualityResult, NotFound, Error>>(
                    nhsPersonId!
                )
            );

        // Act
        var result = await _sut.FindPersonIdAsync(
            specification: personSpec,
            clientId: "test-client-id",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<PlainPersonId>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnPlainPersonId_WhenGlobalEncryptionDisabled_AndEncryptionKeyExists()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        _encryptionConfiguration.Value.Returns(
            new EncryptionConfiguration() { EnablePersonIdEncryption = false }
        );
        _custodianService
            .GetCustodianAsync("test-client-id")
            .Returns(
                Domain.Models.Result<ProviderDefinition>.Ok(
                    new ProviderDefinition()
                    {
                        Encryption = new EncryptionDefinition() { Key = "some-encryption-key" },
                    }
                )
            );
        var nhsPersonId = NhsPersonId.Create("9999999999").Value;
        _matchingService
            .MatchPersonAsync(personSpec, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<OneOf<NhsPersonId, DataQualityResult, NotFound, Error>>(
                    nhsPersonId!
                )
            );

        // Act
        var result = await _sut.FindPersonIdAsync(
            specification: personSpec,
            clientId: "test-client-id",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<PlainPersonId>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnPlainPersonId_WhenGlobalEncryptionEnabled_AndEncryptionDoesNotKeyExist()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        _encryptionConfiguration.Value.Returns(
            new EncryptionConfiguration() { EnablePersonIdEncryption = true }
        );
        _custodianService
            .GetCustodianAsync("test-client-id")
            .Returns(
                Domain.Models.Result<ProviderDefinition>.Ok(
                    new ProviderDefinition()
                    {
                        Encryption = new EncryptionDefinition() { Key = null! },
                    }
                )
            );
        var nhsPersonId = NhsPersonId.Create("9999999999").Value;
        _matchingService
            .MatchPersonAsync(personSpec, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<OneOf<NhsPersonId, DataQualityResult, NotFound, Error>>(
                    nhsPersonId!
                )
            );

        // Act
        var result = await _sut.FindPersonIdAsync(
            specification: personSpec,
            clientId: "test-client-id",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<PlainPersonId>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnEncryptedPersonId_WhenGlobalEncryptionEnabled_AndEncryptionKeyExists()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        _encryptionConfiguration.Value.Returns(
            new EncryptionConfiguration() { EnablePersonIdEncryption = true }
        );
        _custodianService
            .GetCustodianAsync("test-client-id")
            .Returns(
                Domain.Models.Result<ProviderDefinition>.Ok(
                    new ProviderDefinition()
                    {
                        Encryption = new EncryptionDefinition() { Key = "some-encryption-key" },
                    }
                )
            );
        var nhsPersonId = NhsPersonId.Create("9999999999").Value;
        _matchingService
            .MatchPersonAsync(personSpec, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<OneOf<NhsPersonId, DataQualityResult, NotFound, Error>>(
                    nhsPersonId!
                )
            );

        _encryptionService
            .EncryptNhsToPersonId(nhsPersonId!.Value, Arg.Any<EncryptionDefinition>())
            .Returns(Domain.Models.Result<string>.Ok("encrypted-person-id-value"));

        // Act
        var result = await _sut.FindPersonIdAsync(
            specification: personSpec,
            clientId: "test-client-id",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<EncryptedSuidPersonId>(result.Value);
    }

    [Fact]
    public async Task ShouldPassthroughRemainder_WhenMatchingServiceReturnsDataQualityResult()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        var dataQualityResult = new DataQualityResult
        {
            Given = QualityType.NotProvided,
            Family = QualityType.Valid,
            BirthDate = QualityType.Valid,
        };
        _matchingService
            .MatchPersonAsync(personSpec, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<OneOf<NhsPersonId, DataQualityResult, NotFound, Error>>(
                    dataQualityResult
                )
            );

        // Act
        var result = await _sut.FindPersonIdAsync(
            specification: personSpec,
            clientId: "test-client-id",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<DataQualityResult>(result.Value);
    }

    [Fact]
    public async Task ShouldPassthroughRemainder_WhenMatchingServiceReturnsNotFound()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        _matchingService
            .MatchPersonAsync(personSpec, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<OneOf<NhsPersonId, DataQualityResult, NotFound, Error>>(
                    new NotFound()
                )
            );

        // Act
        var result = await _sut.FindPersonIdAsync(
            specification: personSpec,
            clientId: "test-client-id",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task ShouldPassthroughRemainder_WhenMatchingServiceReturnsError()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        _matchingService
            .MatchPersonAsync(personSpec, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<OneOf<NhsPersonId, DataQualityResult, NotFound, Error>>(new Error())
            );

        // Act
        var result = await _sut.FindPersonIdAsync(
            specification: personSpec,
            clientId: "test-client-id",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<Error>(result.Value);
    }

    private static PersonSpecification CreateMinimalValidPersonSpec()
    {
        return new PersonSpecification
        {
            Given = "Jon",
            Family = "Doe",
            BirthDate = new DateOnly(DateTime.Now.AddYears(-10).Year, 1, 1),
        };
    }

    [Fact]
    public async Task ShouldReturnError_WhenEncryptionFails()
    {
        // Arrange
        var personSpec = CreateMinimalValidPersonSpec();
        _encryptionConfiguration.Value.Returns(
            new EncryptionConfiguration() { EnablePersonIdEncryption = true }
        );
        _custodianService
            .GetCustodianAsync("test-client-id")
            .Returns(
                Domain.Models.Result<ProviderDefinition>.Ok(
                    new ProviderDefinition()
                    {
                        Encryption = new EncryptionDefinition() { Key = "some-encryption-key" },
                    }
                )
            );
        var nhsPersonId = NhsPersonId.Create("9999999999").Value;
        _matchingService
            .MatchPersonAsync(personSpec, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult<OneOf<NhsPersonId, DataQualityResult, NotFound, Error>>(
                    nhsPersonId!
                )
            );

        // Simulate encryption failure (Success = false, Value = null)
        _encryptionService
            .EncryptNhsToPersonId(nhsPersonId!.Value, Arg.Any<EncryptionDefinition>())
            .Returns(Domain.Models.Result<string>.Fail("encryption failed"));

        // Act
        var result = await _sut.FindPersonIdAsync(
            specification: personSpec,
            clientId: "test-client-id",
            CancellationToken.None
        );

        // Assert
        Assert.IsType<Error>(result.Value);
    }
}
