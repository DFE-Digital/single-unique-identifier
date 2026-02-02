using System.ComponentModel.DataAnnotations;

namespace SUI.Find.FindApi.Configurations;

public class MatchFunctionConfiguration
{
    public const string SectionName = "MatchFunction";

    [Required]
    public required string XApiKey { get; set; }
}
