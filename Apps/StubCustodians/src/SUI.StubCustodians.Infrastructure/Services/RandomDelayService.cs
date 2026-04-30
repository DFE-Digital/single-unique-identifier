using System.Diagnostics.CodeAnalysis;
using SUI.StubCustodians.Application.Interfaces;

namespace SUI.StubCustodians.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class RandomDelayService : IRandomDelayService
{
    private readonly int _minMs;
    private readonly int _maxMs;

    public RandomDelayService(float minSeconds, float maxSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minSeconds);

        ArgumentOutOfRangeException.ThrowIfLessThan(maxSeconds, minSeconds);

        _minMs = (int)(minSeconds * 1000);
        _maxMs = (int)(maxSeconds * 1000);
    }

    public Task DelayAsync(CancellationToken ct)
    {
        var ms = Random.Shared.Next(_minMs, _maxMs + 1);
        return Task.Delay(ms, ct);
    }
}
