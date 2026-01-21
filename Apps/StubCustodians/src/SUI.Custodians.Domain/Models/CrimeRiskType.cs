using System.Text.Json.Serialization;

namespace SUI.Custodians.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CrimeRiskType
{
    SexualExploitation,
    CriminalExploitation,
    Radicalisation,
    ModernSlaveryAndTrafficking,
    GangsAndYouthViolence,
}
