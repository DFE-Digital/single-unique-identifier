namespace SUI.Custodians.Domain.Models;

public record CrimeMissingEpisodeV1
{
    /// <summary>
    /// Missing episode date
    /// </summary>
    public DateOnly? Date { get; init; }
}
