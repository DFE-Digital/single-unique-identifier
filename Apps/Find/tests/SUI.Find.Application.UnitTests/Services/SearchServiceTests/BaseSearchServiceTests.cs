using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
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
    protected readonly ISearchResultEntryRepository SearchResultEntryRepository =
        Substitute.For<ISearchResultEntryRepository>();

    protected BaseSearchServiceTests()
    {
        Sut = Substitute.ForPartsOf<SearchService>(
            Logger,
            CustodianService,
            HashService,
            SearchResultEntryRepository
        );
        var metaData = new SearchJobMetadata("test-person-id", DateTime.UtcNow, "invocation-id");
        var policyData = new PolicyContext("test-org-id", "SAFEGUARDING", "LOCAL_AUTHORITY");
        Sut.ReadOrchestratorInput<SearchOrchestratorInput>(Arg.Any<OrchestrationMetadata>())
            .Returns(new SearchOrchestratorInput("test-suid", metaData, policyData));
    }
}
