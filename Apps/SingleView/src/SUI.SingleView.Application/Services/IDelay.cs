namespace SUI.SingleView.Application.Services;

/// <summary>
/// Abstraction for delaying execution to allow faking in tests.
/// </summary>
public interface IDelay
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}

public sealed class SystemDelay : IDelay
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default) =>
        Task.Delay(delay, cancellationToken);
}
