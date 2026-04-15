namespace SUI.StubCustodians.Application.Interfaces;

public interface IDelayService
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}
