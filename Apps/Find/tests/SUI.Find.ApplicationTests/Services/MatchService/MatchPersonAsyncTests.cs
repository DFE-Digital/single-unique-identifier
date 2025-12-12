using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OneOf.Types;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.Domain.Models;
using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.ApplicationTests.Services.MatchService;

public class MatchPersonAsyncTests
{
    private readonly IMatchRepository _matchRepository = Substitute.For<IMatchRepository>();
    private readonly ICustodianService _custodianService = Substitute.For<ICustodianService>();
    private readonly IPersonIdEncryptionService _personIdEncryptionService =
        Substitute.For<IPersonIdEncryptionService>();
    private readonly MatchingService _service;
    private readonly MatchPersonRequest _request;
    private const string ClientId = "test-client-id";

    public MatchPersonAsyncTests()
    {
        _service = new MatchingService(
            Substitute.For<ILogger<MatchingService>>(),
            _matchRepository,
            _custodianService,
            _personIdEncryptionService
        );
        _request = new MatchPersonRequest
        {
            Given = "John",
            Family = "Doe",
            BirthDate = new DateOnly(1990, 1, 1),
        };
    }

    [Fact]
    public async Task ShouldReturnMatch_WhenPersonIsFound()
    {
        var personId = EncryptedPersonId.Create("Cy13hyZL-4LSIwVy50p-Hg");
        _matchRepository
            .MatchPersonAsync(_request)
            .Returns(new MatchFhirResponse.Match(personId.Value));
        _custodianService
            .GetCustodianAsync(ClientId)
            .Returns(
                Domain.Models.Result<ProviderDefinition>.Ok(
                    new ProviderDefinition
                    {
                        OrgId = ClientId,
                        OrgName = "Test Organisation",
                        Encryption = new EncryptionDefinition { Key = "test-key" },
                    }
                )
            );
        _personIdEncryptionService
            .EncryptNhsToPersonId(Arg.Any<string>(), Arg.Any<EncryptionDefinition>())
            .Returns(Domain.Models.Result<string>.Ok(personId.Value));

        var result = await _service.MatchPersonAsync(_request, ClientId);

        var val = Assert.IsType<EncryptedPersonId>(result.Value);
        Assert.Equal(personId, val);
    }

    [Fact]
    public async Task ShouldReturnNoMatch_WhenPersonIsNotFound()
    {
        _matchRepository.MatchPersonAsync(_request).Returns(new NotFound());
        _custodianService
            .GetCustodianAsync(ClientId)
            .Returns(Domain.Models.Result<ProviderDefinition>.Fail("Not found"));

        var result = await _service.MatchPersonAsync(_request, ClientId);

        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnError_WhenExceptionIsThrown()
    {
        _custodianService.GetCustodianAsync(ClientId).Throws(new Exception("error"));

        var result = await _service.MatchPersonAsync(_request, ClientId);

        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnError_WhenEncryptionDefinitionIsMissing()
    {
        _matchRepository.MatchPersonAsync(_request).Returns("nhs-number-123");
        _custodianService
            .GetCustodianAsync(ClientId)
            .Returns(
                Domain.Models.Result<ProviderDefinition>.Ok(
                    new ProviderDefinition
                    {
                        OrgId = ClientId,
                        OrgName = "Test Organisation",
                        Encryption = null, // Missing encryption definition
                    }
                )
            );

        var result = await _service.MatchPersonAsync(_request, ClientId);

        Assert.IsType<Error>(result.Value);
    }
}
