using SUi.Find.Application.Validation;
using SUI.Find.Domain.Enums;

namespace SUi.Find.Application.Models;

public class PersonMatchResponse
{
    public required MatchResult Result { get; set; }
    public required DataQualityResult DataQuality { get; set; }
}

public class MatchResult
{
    public MatchStatus MatchStatus { get; }
    public string? NhsNumber { get; }
    public string? ProcessStage { get; }
    public decimal? Score { get; }
    public string? ErrorMessage { get; }

    private MatchResult(MatchStatus status,
        decimal? score = null,
        string? processStage = null,
        string? nhsNumber = null,
        string? errorMessage = null)
    {
        MatchStatus = status;
        Score = score;
        ProcessStage = processStage;
        NhsNumber = nhsNumber;
        ErrorMessage = errorMessage;
    }

    public static MatchResult Error(string message) =>
        new(MatchStatus.Error, errorMessage: message);

    public static MatchResult NoMatch() =>
        new(MatchStatus.NoMatch);

    public static MatchResult ManyMatch(string stage) =>
        new(MatchStatus.ManyMatch, processStage: stage);

    public static MatchResult PotentialMatch(decimal score, string stage, string nhsNumber) =>
        new(MatchStatus.PotentialMatch, score, stage, nhsNumber);

    public static MatchResult Match(decimal score, string stage, string nhsNumber) =>
        new(MatchStatus.Match, score, stage, nhsNumber);

    /// <summary>
    /// Set order is explicit away from MatchStatus enum order to preserve logic if enum changes
    /// </summary>
    /// <param name="status">status </param>
    /// <returns>Highest rated score</returns>
    private static int GetPriority(MatchStatus status) => status switch
    {
        MatchStatus.Match => 3,
        MatchStatus.PotentialMatch => 2,
        MatchStatus.ManyMatch => 1,
        MatchStatus.NoMatch => 0,
        _ => -1
    };

    public bool IsBetterThan(MatchResult? other)
    {
        if (other is null) return true;

        var currentPriority = GetPriority(MatchStatus);
        var otherPriority = GetPriority(other.MatchStatus);

        if (currentPriority != otherPriority)
            return currentPriority > otherPriority;

        return (Score ?? -1) > (other.Score ?? -1);
    }
}

