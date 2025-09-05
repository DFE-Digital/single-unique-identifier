namespace SUi.Find.Application.Models;

public class PersonMatchResponse
{
    public required MatchResult Result { get; set; }
    public required MatchDataQuality DataQuality { get; set; }

    public class MatchResult
    {
        public required string MatchStatus { get; set; }
        public string? MatchStatusErrorMessage { get; set; } // only if there is an error  "GivenName not provided"
        public string? NhsNumber { get; set; }
        public required string ProcessStage { get; set; } = string.Empty;
        public decimal? Score { get; set; }
    }

    public class MatchDataQuality
    {
        public required string Given { get; set; }
        public required string Family { get; set; }
        public required string Birthdate { get; set; }
        public required string AddressPostalCode { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }
        public required string Gender { get; set; }
    }
}

