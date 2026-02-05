using System.Text.Json.Serialization;

namespace SUI.Custodians.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HealthSetting
{
    Other,
    Hospital,
    GP,
    Community,
}
