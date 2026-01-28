namespace SUI.StubCustodians.Application.Interfaces;

public interface IRandomDelayService
{
    Task DelayAsync(CancellationToken ct);
}
