namespace SUI.Custodians.Domain.Models;

public record CrimeMissingEpisodeV1
{
    /// <summary>
    /// Missing episode date
    /// </summary>
    /// <example>2025-06-12</example>
    public DateOnly? Date { get; init; }

    /// <summary>
    /// Missing episode returned home interview attended
    /// </summary>
    /// <example>true</example>
    public bool? ReturnedHomeInterviewAttended { get; init; }
}
