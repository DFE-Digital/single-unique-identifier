using SUI.StubCustodians.Application.Services;

namespace SUI.StubCustodians.Application.Unit.Tests.Services;

public class SystemDelayServiceTests
{
    private readonly SystemDelayService _service = new();

    [Fact]
    public async Task DelayAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(10);

        // Act
        await _service.DelayAsync(delay);

        // Assert
        Assert.True(true); // If it reaches here, the delay completed without exception
    }

    [Fact]
    public async Task DelayAsync_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _service.DelayAsync(TimeSpan.FromSeconds(1), cts.Token)
        );
    }

    [Fact]
    public async Task DelayAsync_ShouldDelayApproximatelyRequestedTime()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(50);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _service.DelayAsync(delay);

        sw.Stop();

        // Assert
        Assert.True(sw.Elapsed >= delay);
    }
}
