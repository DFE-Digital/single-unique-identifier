namespace Interfaces;

public interface IRandomDelayService
{
    Task DelayAsync(CancellationToken ct);
}
