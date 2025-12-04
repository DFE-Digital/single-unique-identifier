using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
        var personId = new EncryptedPersonId("encrypted-id");
        _matchRepository
            .MatchPersonAsync(_request)
            .Returns(new MatchFhirResponse.Match(personId.EncryptedValue));
        _custodianService
            .GetCustodianAsync(ClientId)
            .Returns(
                Result<ProviderDefinition>.Ok(
                    new ProviderDefinition
                    {
                        OrgId = ClientId,
                        OrgName = "Test Organisation",
                        Encryption = new EncryptionDefinition { Key = "test-key" },
                    }
                )
            );
        _personIdEncryptionService
            .EncryptNhsToPersonId(personId.EncryptedValue, Arg.Any<EncryptionDefinition>())
            .Returns(Result<string>.Ok("encrypted-id"));

        var result = await _service.MatchPersonAsync(_request, ClientId);

        Assert.IsType<MatchPersonResponse.Match>(result);
        Assert.Equal(personId, ((MatchPersonResponse.Match)result).PersonId);
    }

    [Fact]
    public async Task ShouldReturnNoMatch_WhenPersonIsNotFound()
    {
        _matchRepository.MatchPersonAsync(_request).Returns(new MatchFhirResponse.NoMatch());
        _custodianService
            .GetCustodianAsync(ClientId)
            .Returns(Result<ProviderDefinition>.Fail("Not found"));

        var result = await _service.MatchPersonAsync(_request, ClientId);

        Assert.IsType<MatchPersonResponse.NoMatch>(result);
    }

    [Fact]
    public async Task ShouldReturnError_WhenExceptionIsThrown()
    {
        _custodianService.GetCustodianAsync(ClientId).Throws(new Exception("error"));

        var result = await _service.MatchPersonAsync(_request, ClientId);

        var error = Assert.IsType<MatchPersonResponse.Error>(result);
        Assert.False(string.IsNullOrEmpty(error.ErrorMessage));
    }

    [Fact]
    public async Task ShouldReturnError_WhenEncryptionDefinitionIsMissing()
    {
        _matchRepository
            .MatchPersonAsync(_request)
            .Returns(new MatchFhirResponse.Match("nhs-number-123"));
        _custodianService
            .GetCustodianAsync(ClientId)
            .Returns(
                Result<ProviderDefinition>.Ok(
                    new ProviderDefinition
                    {
                        OrgId = ClientId,
                        OrgName = "Test Organisation",
                        Encryption = null, // Missing encryption definition
                    }
                )
            );

        var result = await _service.MatchPersonAsync(_request, ClientId);

        var errorResult = Assert.IsType<MatchPersonResponse.Error>(result);
        Assert.Equal("Encryption definition not found.", errorResult.ErrorMessage);
    }
}
