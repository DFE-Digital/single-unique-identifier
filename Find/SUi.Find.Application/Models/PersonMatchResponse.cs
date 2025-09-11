using SUi.Find.Application.Validation;

namespace SUi.Find.Application.Models;

public class PersonMatchResponse
{
    public required MatchResult Result { get; set; }
    public required DataQualityResult DataQuality { get; set; }
}


