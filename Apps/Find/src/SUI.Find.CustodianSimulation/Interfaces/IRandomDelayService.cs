namespace SUI.Find.CustodianSimulation.Interfaces;

public interface IRandomDelayService
{
    Task DelayAsync(CancellationToken ct);
}
