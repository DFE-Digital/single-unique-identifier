using SUI.Matching.Application.Validation;

namespace SUI.Matching.Application.Models;

public class PersonMatchResponse
{
    public required MatchResult Result { get; set; }
    public required DataQualityResult DataQuality { get; set; }
}
