using SUI.StubCustodians.Application.Interfaces;

namespace SUI.StubCustodians.Application.Services;

public class SystemDelayService : IDelayService
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default) =>
        Task.Delay(delay, cancellationToken);
}
