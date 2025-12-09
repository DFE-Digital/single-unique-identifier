using Shouldly;
using SUI.SingleView.Application.Services;
using Xunit;

namespace SUI.SingleView.Application.UnitTests.Services;

public class SystemDelayTests
{
    private readonly SystemDelay _sut = new();

    [Fact]
    public async Task DelayAsync_CompletesWithoutCancellation()
    {
        // Act / Assert (should not throw)
        await _sut.DelayAsync(TimeSpan.FromMilliseconds(20), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DelayAsync_HonorsCancellationToken()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken
        );

        var task = _sut.DelayAsync(TimeSpan.FromSeconds(5), cts.Token);
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () => await task);
    }
}
