using System.Text.Json.Serialization;

namespace SUI.Custodians.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CrimeRiskTypeV1
{
    SexualExploitation,
    CriminalExploitation,
    Radicalisation,
    ModernSlaveryAndTrafficking,
    GangsAndYouthViolence,
}
