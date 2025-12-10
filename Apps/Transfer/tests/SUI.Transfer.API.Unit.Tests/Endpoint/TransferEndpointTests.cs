using NSubstitute;
using SUI.Transfer.API.Endpoint;
using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

namespace SUI.Transfer.API.Unit.Tests.Endpoint;

public class TransferEndpointTests
{
    private readonly ITransferService _mockTransferService = Substitute.For<ITransferService>();

    [Fact]
    public void Transfer_ReturnsQueuedJob()
    {
        //Arrange
        var testId = "999-000-1234";
        var createdAt = TimeProvider.System.GetUtcNow();
        var mockResponse = new QueuedTransferJobState(Guid.NewGuid(), testId, createdAt);

        _mockTransferService.BeginTransferJob(Arg.Any<string>()).Returns(mockResponse);

        // Act
        var result = TransferEndpoint.Transfer(testId, _mockTransferService);

        // Assert
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Value);
        Assert.Equal(
            new QueuedTransferJobState(mockResponse.JobId, testId, createdAt)
            {
                LastUpdatedAt = result.Value.LastUpdatedAt,
            },
            result.Value
        );
    }

    [Fact]
    public void CancelTransferJob_ThrowsNotImplementedException()
    {
        var testId = Guid.Empty;

        Assert.Throws<NotImplementedException>(() => TransferEndpoint.CancelTransferJob(testId));
    }
}
