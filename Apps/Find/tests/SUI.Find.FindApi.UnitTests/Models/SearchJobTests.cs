using SUI.Find.Application.Enums;
using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.UnitTests.Models;

public class SearchJobTests
{
    [Fact]
    public void ShouldCreateAllLinksWhenCompleted()
    {
        // Arrange
        var jobId = "test-job-id";
        var searchJob = new SearchJob { JobId = jobId, Status = SearchStatus.Completed };

        // Act
        var links = searchJob.Links;

        // Assert
        Assert.Equal(4, links.Count);
        Assert.True(links.ContainsKey("self"));
        Assert.True(links.ContainsKey("status"));
        Assert.True(links.ContainsKey("cancel"));
        Assert.True(links.ContainsKey("results"));
    }

    [Fact]
    public void ShouldContainJobIdInLinkValuesWhenCompleted()
    {
        // Arrange
        var jobId = "test-job-id";
        var searchJob = new SearchJob { JobId = jobId, Status = SearchStatus.Completed };

        // Act
        var links = searchJob.Links;

        // Assert
        Assert.Contains(links.Values, link => link.Href.Contains(jobId));
    }

    [Fact]
    public void ShouldNotCreateResultsLinkWhenNotCompleted()
    {
        // Arrange
        var jobId = "test-job-id";
        var searchJob = new SearchJob { JobId = jobId, Status = SearchStatus.Running };

        // Act
        var links = searchJob.Links;

        // Assert
        Assert.Equal(3, links.Count);
        Assert.True(links.ContainsKey("self"));
        Assert.True(links.ContainsKey("status"));
        Assert.True(links.ContainsKey("cancel"));
        Assert.False(links.ContainsKey("results"));
    }
}
