using System.Text.Json.Serialization;

namespace SUI.UIHarness.Web.Models.Records;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HealthSetting
{
    Other,
    Hospital,
    GP,
    Community,
}
