using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;

namespace SUI.Find.ApplicationTests.Services.SearchServiceTests;

public class BaseSearchService
{
    protected readonly SearchService Sut;
    protected readonly ILogger<SearchService> Logger = Substitute.For<ILogger<SearchService>>();
    protected readonly ICustodianService CustodianService = Substitute.For<ICustodianService>();
    protected readonly IHashService HashService = Substitute.For<IHashService>();
    protected readonly IPersonIdEncryptionService EncryptionService =
        Substitute.For<IPersonIdEncryptionService>();

    protected BaseSearchService()
    {
        Sut = Substitute.ForPartsOf<SearchService>(
            Logger,
            EncryptionService,
            CustodianService,
            HashService
        );

        var metaData = new SearchJobMetadata("test-person-id", DateTime.UtcNow, "invocation-id");
        var policyData = new PolicyContext("test-client-id", []);
        Sut.ReadOrchestratorInput<SearchOrchestratorInput>(Arg.Any<OrchestrationMetadata>())
            .Returns(new SearchOrchestratorInput("test-suid", metaData, policyData));
    }
}
