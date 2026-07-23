using System.ComponentModel.DataAnnotations;

namespace SUI.GetAnIdentifier.Function.Configuration;

public class GetAnIdentifierConfiguration
{
    public const string SectionName = "GetAnIdentifierFunction";

    [Required]
    public required string XApiKey { get; set; }
}
