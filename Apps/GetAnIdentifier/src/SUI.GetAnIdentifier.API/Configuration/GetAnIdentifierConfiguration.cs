using System.ComponentModel.DataAnnotations;

namespace SUI.GetAnIdentifier.API.Configuration;

public class GetAnIdentifierConfiguration
{
    public const string SectionName = "GetAnIdentifierFunction";

    [Required]
    public required string XApiKey { get; set; }
}
