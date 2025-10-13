using SUI.Find.Application.Validation;

namespace SUI.Find.Application.Models;

public class PersonMatchResponse
{
    public required MatchResult Result { get; set; }
    public required DataQualityResult DataQuality { get; set; }
}


