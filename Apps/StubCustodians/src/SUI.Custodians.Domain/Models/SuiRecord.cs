using System.Text.Json.Serialization;

namespace SUI.Custodians.Domain.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(
    typeof(ChildrensServicesDetailsRecordV1),
    typeDiscriminator: "ChildrensServicesDetailsRecordV1"
)]
[JsonDerivedType(typeof(CrimeDataRecordV1), typeDiscriminator: "CrimeDataRecordV1")]
[JsonDerivedType(typeof(EducationDetailsRecordV1), typeDiscriminator: "EducationDetailsRecordV1")]
[JsonDerivedType(typeof(HealthDataRecordV1), typeDiscriminator: "HealthDataRecordV1")]
[JsonDerivedType(typeof(PersonalDetailsRecordV1), typeDiscriminator: "PersonalDetailsRecordV1")]
public record SuiRecord
{
    public string Test { get; set; }
}
