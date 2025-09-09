namespace SUi.Find.Application.Models;

public class PersonMatchResponse
{
    public required MatchResult Result { get; set; }
    public required DataQualityResult DataQuality { get; set; }
}

public class MatchResult
{
    public MatchStatus MatchStatus { get; set; }

    public string? NhsNumber { get; set; }

    public string? ProcessStage { get; set; }

    public decimal? Score { get; set; }
    public string? MatchStatusErrorMessage { get; set; }
    
    
    public MatchResult(MatchStatus status) => MatchStatus = status;

    public MatchResult(MatchStatus status, string errorMessage)
    {
        MatchStatus = status;
        MatchStatusErrorMessage = errorMessage;
    }
    
    public MatchResult(MatchStatus status, decimal? score, string processStage)
    {
        MatchStatus = status;
        Score = score;
        ProcessStage = processStage;
    }
    
    public MatchResult(MatchStatus status, decimal? score, string processStage, string? nhsNumber)
    {
        MatchStatus = status;
        Score = score;
        ProcessStage = processStage;
        NhsNumber = nhsNumber;
    }
}

