using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SUI.Find.Application.Models;

// Mimicks Pilot 1 MatchPersonRequest
public record MatchPersonRequest
{
    [Required]
    [StringLength(35)]
    [JsonPropertyName("given")]
    public string Given { get; set; } = null!;

    [Required]
    [StringLength(35)]
    [JsonPropertyName("family")]
    public string Family { get; set; } = null!;

    [Required]
    [DataType(DataType.Date)]
    [JsonPropertyName("birthdate")]
    public DateOnly? BirthDate { get; set; }

    [AllowedValues("male", "female", "unknown", "other", null)]
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [Phone]
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [EmailAddress]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [RegularExpression(
        "^(([A-Z][0-9]{1,2})|(([A-Z][A-HJ-Y][0-9]{1,2})|(([A-Z][0-9][A-Z])|([A-Z][A-HJ-Y][0-9]?[A-Z])))) [0-9][A-Z]{2}$"
    )]
    [JsonPropertyName("addresspostalcode")]
    public string? AddressPostalCode { get; set; }
}
