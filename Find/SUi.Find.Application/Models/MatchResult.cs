using System.Text.Json.Serialization;
using SUI.Find.Domain.Enums;

namespace SUi.Find.Application.Models;

public class MatchResult(
    MatchStatus matchStatus,
    decimal? score = null,
    string? processStage = null,
    string? nhsNumber = null,
    string? errorMessage = null)
{
    public MatchStatus MatchStatus { get; } = matchStatus;
    public string? NhsNumber { get; } = nhsNumber;
    public string? ProcessStage { get; } = processStage;
    public decimal? Score { get; } = score;
    public string? ErrorMessage { get; } = errorMessage;

    public static MatchResult Error(string message)
    {
        return new MatchResult(MatchStatus.Error, errorMessage: message);
    }

    public static MatchResult NoMatch()
    {
        return new MatchResult(MatchStatus.NoMatch);
    }

    public static MatchResult ManyMatch(string stage)
    {
        return new MatchResult(MatchStatus.ManyMatch, processStage: stage);
    }

    public static MatchResult PotentialMatch(decimal score, string stage, string nhsNumber)
    {
        return new MatchResult(MatchStatus.PotentialMatch, score, stage, nhsNumber);
    }

    public static MatchResult Match(decimal score, string stage, string nhsNumber)
    {
        return new MatchResult(MatchStatus.Match, score, stage, nhsNumber);
    }

    /// <summary>
    /// Set order is explicit away from MatchStatus enum order to preserve logic if enum changes
    /// </summary>
    /// <param name="matchStatus">status </param>
    /// <returns>Highest rated score</returns>
    private static int GetPriority(MatchStatus matchStatus)
    {
        return matchStatus switch
        {
            MatchStatus.Match => 3,
            MatchStatus.PotentialMatch => 2,
            MatchStatus.ManyMatch => 1,
            MatchStatus.NoMatch => 0,
            _ => -1
        };
    }

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