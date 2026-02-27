using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SUI.Find.Application.Configurations;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;

namespace SUI.Find.Application.UnitTests.Services.SearchServiceTests;

public class BaseSearchServiceTests
{
    protected readonly SearchService Sut;
    protected readonly ILogger<SearchService> Logger = Substitute.For<ILogger<SearchService>>();
    protected readonly ICustodianService CustodianService = Substitute.For<ICustodianService>();
    protected readonly IHashService HashService = Substitute.For<IHashService>();
    protected readonly IPersonIdEncryptionService EncryptionService =
        Substitute.For<IPersonIdEncryptionService>();
    protected readonly IOptions<EncryptionConfiguration> EncryptionConfig = Substitute.For<
        IOptions<EncryptionConfiguration>
    >();
    protected readonly ISearchResultEntryRepository SearchResultEntryRepository =
        Substitute.For<ISearchResultEntryRepository>();

    protected BaseSearchServiceTests()
    {
        Sut = Substitute.ForPartsOf<SearchService>(
            Logger,
            EncryptionService,
            CustodianService,
            HashService,
            EncryptionConfig,
            SearchResultEntryRepository
        );
        EncryptionConfig.Value.Returns(
            new EncryptionConfiguration { EnablePersonIdEncryption = true }
        );
        var metaData = new SearchJobMetadata("test-person-id", DateTime.UtcNow, "invocation-id");
        var policyData = new PolicyContext("test-client-id", [], "SAFEGUARDING", "LOCAL_AUTHORITY");
        Sut.ReadOrchestratorInput<SearchOrchestratorInput>(Arg.Any<OrchestrationMetadata>())
            .Returns(new SearchOrchestratorInput("test-suid", metaData, policyData));
    }
}
