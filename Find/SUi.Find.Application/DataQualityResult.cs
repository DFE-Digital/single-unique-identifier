using System.Reflection;
using System.Text.Json.Serialization;
using SUI.Find.Domain.Enums;

namespace SUi.Find.Application;

public class DataQualityResult
{
    public QualityType Given { get; set; } = QualityType.Valid;

    public QualityType Family { get; set; } = QualityType.Valid;

    public QualityType BirthDate { get; set; } = QualityType.Valid;

    public QualityType AddressPostalCode { get; set; } = QualityType.Valid;

    public QualityType Phone { get; set; } = QualityType.Valid;

    public QualityType Email { get; set; } = QualityType.Valid;

    public QualityType Gender { get; set; } = QualityType.Valid;

    public Dictionary<string, string> ToDictionary()
    {
        var jsonPropertyDict = new Dictionary<string, string>();

        var properties = GetType().GetProperties();

        foreach (var property in properties)
        {
            var jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();

            if (jsonPropertyAttribute == null) continue;

            var jsonPropertyName = jsonPropertyAttribute.Name;
            var value = property.GetValue(this);

            if (value is QualityType qualityType)
            {
                jsonPropertyDict[jsonPropertyName] = qualityType.ToString();
            }
        }

        return jsonPropertyDict;
    }
}