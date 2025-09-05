using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SUi.Find.Application.Models;

public class PersonSpecification
{
    [Required]
    [JsonPropertyName("given")]
    public string? Given { get; set; }
    [Required]
    [JsonPropertyName("family")]
    public string? Family { get; set; }
    [Required]
    [JsonPropertyName("birthdate")]
    public DateOnly? BirthDate { get; set; }
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    [JsonPropertyName("addresspostalcode")]
    public string? AddressPostalCode { get; set; }
}