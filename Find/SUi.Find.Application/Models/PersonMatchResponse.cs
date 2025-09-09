namespace SUi.Find.Application.Models;

public class PersonMatchResponse
{
    public required MatchResult Result { get; set; }
    public required DataQualityResult DataQuality { get; set; }
}

public class MatchResult
{
    public string Status { get; set; }
    public SearchResult? Result { get; set; }
    public string? MatchStatusErrorMessage { get; set; } // only if there is an error  "GivenName not provided"
    public string? ProcessStage { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    
    public MatchResult(MatchStatus status) => Status = status.ToString();

    public MatchResult(MatchStatus status, string errorMessage)
    {
        Status = status.ToString();
        MatchStatusErrorMessage = errorMessage;
    }
    public MatchResult(SearchResult result, MatchStatus status, string processStage)
    {
        Result = result;
        Status = status.ToString();
        ProcessStage = processStage;
    }
    
    public MatchResult(SearchResult result, MatchStatus status, decimal? score, string processStage)
    {
        Result = result;
        Status = status.ToString();
        Score = score;
        ProcessStage = processStage;
    }
}

