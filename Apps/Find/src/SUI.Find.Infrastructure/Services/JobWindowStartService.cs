using Microsoft.Extensions.Options;
using SUI.Find.Application.Interfaces;
using SUI.Find.Infrastructure.Configuration;

namespace SUI.Find.Infrastructure.Services;

public class JobWindowStartService(
    IOptionsMonitor<JobClaimConfig> options,
    TimeProvider timeProvider
) : IJobWindowStartService
{
    public DateTimeOffset GetWindowStart()
    {
        var utcNow = timeProvider.GetUtcNow();
        var availableJobWindowStartOffsetHours = options
            .CurrentValue
            .AvailableJobWindowStartOffsetHours;

        return utcNow.AddHours(-availableJobWindowStartOffsetHours);
    }
}
