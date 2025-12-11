using System.Text.Json.Serialization;

namespace SUI.Custodians.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HealthSettingV1
{
    Other,
    Hospital,
    GP,
    Community,
}
