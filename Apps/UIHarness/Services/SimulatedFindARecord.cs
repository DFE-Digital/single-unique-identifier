using UIHarness.Interfaces;
using UIHarness.Models;

namespace UIHarness.Services;

public sealed class SimulatedFindARecord : IFindARecord
{
    private readonly Random _random = new();

    public async Task<IReadOnlyList<string>> FindRecordTypesAsync(Custodian custodian, string nhsNumber, CancellationToken cancellationToken)
    {
        var delaySeconds = _random.Next(2, 7);
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

        var match = custodian.KnownPeople.FirstOrDefault(kp => string.Equals(kp.NhsNumber, nhsNumber, StringComparison.Ordinal));
        if (match is null)
        {
            return [];
        }

        return match.RecordTypes.ToList();
    }
}