using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Application.Services;

namespace SUI.Find.Application.UnitTests.Services;

public class SearchResultsServiceTests
{
    [Fact]
    public async Task PersistSearchResultsAsync_ShouldOnlyPersistAllowedSearchedResults()
    {
        var mockSearchResultEntryRepository = Substitute.For<ISearchResultEntryRepository>();
        var sut = new SearchResultsService(
            mockSearchResultEntryRepository,
            NullLogger<SearchResultsService>.Instance
        );

        const string sourceOrgId = "sourceOrgId";
        const string destOrgId = "destOrgId";
        IReadOnlyList<PepResultItem<CustodianSearchResultItem>> exampleSearchResults =
        [
            new(
                new CustodianSearchResultItem(
                    "org1",
                    "recordType1",
                    "url1",
                    "sys1",
                    "Org 1",
                    "recordId1"
                ),
                sourceOrgId,
                destOrgId,
                new PolicyDecisionResult { IsAllowed = true, Reason = "example1" }
            ),
            new(
                new CustodianSearchResultItem(
                    "org2",
                    "recordType2",
                    "url2",
                    "sys2",
                    "Org 2",
                    "recordId2"
                ),
                sourceOrgId,
                destOrgId,
                new PolicyDecisionResult { IsAllowed = false, Reason = "example2" }
            ),
        ];

        var timeAtStart = DateTimeOffset.UtcNow;

        // ACT
        var countOfRecordsPersisted = await sut.PersistSearchResultsAsync(
            "example-WorkItemId",
            "example-JobId",
            exampleSearchResults,
            CancellationToken.None
        );

        // ASSERT: SearchResultsRepository should only have been called once, for the allowed item
        Assert.Equal(1, countOfRecordsPersisted);

        await mockSearchResultEntryRepository
            .Received(1)
            .UpsertAsync(Arg.Any<SearchResultEntry>(), Arg.Any<CancellationToken>());

        await mockSearchResultEntryRepository
            .Received(1)
            .UpsertAsync(
                Arg.Is<SearchResultEntry>(e =>
                    e.CustodianId == "org1"
                    && e.SystemId == "sys1"
                    && e.CustodianName == "Org 1"
                    && e.RecordType == "recordType1"
                    && e.RecordUrl == "url1"
                    && e.RecordId == "recordId1"
                    && e.SubmittedAtUtc >= timeAtStart
                    && e.JobId == "example-JobId"
                    && e.WorkItemId == "example-WorkItemId"
                    && e.RequestingOrganisationId == "destOrgId"
                ),
                CancellationToken.None
            );
    }
}
