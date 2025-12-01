namespace SUI.Transfer.Domain;

public record HealthAttendanceSummaries
{
    public required HealthAttendanceSummary? Last12Months { get; init; }

    public required HealthAttendanceSummary? Last5Years { get; init; }
}
