using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;

namespace SUI.Find.Application.Models.Fhir;

public sealed class SearchQuery
{
    private const string DateQueryRegex = @"^(eq||le||ge)\d{4}[-]\d{2}[-]\d{2}$";

    [JsonPropertyName("_fuzzy-match")]
    public bool? FuzzyMatch { get; set; }

    [JsonPropertyName("_exact-match")]
    public bool? ExactMatch { get; set; }

    [JsonPropertyName("_history")]
    public bool? History { get; set; }

    [JsonPropertyName("_max-results")]
    public bool? MaxResults { get; set; }

    [JsonPropertyName("address-postalcode")]
    public string? AddressPostalcode { get; set; }

    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("given")]
    public string[]? Given { get; set; }

    [RegularExpression("male||female||other||unknown", ErrorMessage = "Incorrect format.")]
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [EmailAddress]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [RegularExpression(DateQueryRegex, ErrorMessage = "Incorrect format.")]
    [JsonPropertyName("birthdate")]
    public string[]? Birthdate { get; set; }

    public Dictionary<string, object> ToDictionary()
    {
        var jsonPropertyDict = new Dictionary<string, object>();

        var properties = GetType().GetProperties();

        foreach (var property in properties)
        {
            var jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();

            if (jsonPropertyAttribute == null)
                continue;

            var jsonPropertyName = jsonPropertyAttribute.Name;
            var value = property.GetValue(this);

            if (value != null)
            {
                jsonPropertyDict[jsonPropertyName] = value;
            }
        }

        return jsonPropertyDict;
    }
}
