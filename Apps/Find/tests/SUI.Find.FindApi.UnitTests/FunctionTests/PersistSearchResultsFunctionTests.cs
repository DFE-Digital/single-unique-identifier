using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.FindApi.Functions.ActivityFunctions;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class PersistSearchResultsFunctionTests
{
    [Fact]
    public async Task PersistSearchResults_Test()
    {
        var mockSearchResultsService = Substitute.For<ISearchResultsService>();
        var sut = new PersistSearchResultsFunction(
            mockSearchResultsService,
            Substitute.For<ILogger<PersistSearchResultsFunction>>()
        );

        IReadOnlyList<PepResultItem<CustodianSearchResultItem>> exampleSearchResults = [];

        var input = new PersistSearchResultsInput(
            exampleSearchResults,
            "example-WorkItemId",
            "example-JobId",
            "example-InvocationId",
            "example-RequestingOrdId",
            "example-SourceOrgId"
        );

        // ACT
        await sut.PersistSearchResults(input, CancellationToken.None);

        // ASSERT
        await mockSearchResultsService
            .Received(1)
            .PersistSearchResultsAsync(
                workItemId: "example-WorkItemId",
                jobId: "example-JobId",
                exampleSearchResults,
                CancellationToken.None
            );
    }
}
