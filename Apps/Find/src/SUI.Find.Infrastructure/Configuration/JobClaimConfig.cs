namespace SUI.Find.Infrastructure.Configuration;

public class JobClaimConfig
{
    public const string SectionName = "JobClaim";

    public double AvailableJobWindowStartOffsetHours
    {
        get;
        init => field = Math.Abs(value);
    } = 72;

    public double LeaseDurationMinutes
    {
        get;
        init => field = Math.Abs(value);
    } = 30;

    public int MaxClaimAttemptsPerJob { get; init; } = 5;

    public int MaxReScanAttempts { get; init; } = 10;
}
