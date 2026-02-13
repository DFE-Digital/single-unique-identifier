using UIHarness.Interfaces;
using UIHarness.Models;

namespace UIHarness.Services;

public sealed class SimulatedFindAnId : IFindAnId
{
    private readonly Random _random = new();

    public async Task<string> EnrolAsync(PersonRecord person, CancellationToken cancellationToken)
    {
        var delaySeconds = _random.Next(2, 7);
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
        return person.NhsNumber;
    }
}
