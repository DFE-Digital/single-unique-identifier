using SUI.Transfer.Application.Services;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class TransferServiceTests
{
    [Fact]
    public async Task TransferAsync_ReturnsResponse_WithRequestId()
    {
        // Arrange
        var service = new TransferService();
        var requestId = "999-000-1234";

        // Act
        var response = await service.TransferAsync(requestId);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Result);
        Assert.Equal(response.Result.Id, requestId);
    }
}
