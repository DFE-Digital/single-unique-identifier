using System.Diagnostics.CodeAnalysis;
using SUI.Find.CustodianSimulation.Interfaces;

namespace SUI.Find.CustodianSimulation.Services;

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class RandomDelayService : IRandomDelayService
{
    private readonly int _minMs;
    private readonly int _maxMs;

    public RandomDelayService(int minSeconds, int maxSeconds)
    {
        if (minSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(minSeconds));
        if (maxSeconds < minSeconds)
            throw new ArgumentOutOfRangeException(nameof(maxSeconds));

        _minMs = minSeconds * 1000;
        _maxMs = maxSeconds * 1000;
    }

    public Task DelayAsync(CancellationToken ct)
    {
        var ms = Random.Shared.Next(_minMs, _maxMs + 1);
        return Task.Delay(ms, ct);
    }
}
