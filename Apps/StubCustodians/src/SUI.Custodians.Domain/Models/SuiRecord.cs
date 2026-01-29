using System.Text.Json.Serialization;

namespace SUI.Custodians.Domain.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(
    typeof(ChildrensServicesDetailsRecord),
    typeDiscriminator: nameof(ChildrensServicesDetailsRecord)
)]
[JsonDerivedType(typeof(CrimeDataRecord), typeDiscriminator: nameof(CrimeDataRecord))]
[JsonDerivedType(typeof(EducationDetailsRecord), typeDiscriminator: nameof(EducationDetailsRecord))]
[JsonDerivedType(typeof(HealthDataRecord), typeDiscriminator: nameof(HealthDataRecord))]
[JsonDerivedType(typeof(PersonalDetailsRecord), typeDiscriminator: nameof(PersonalDetailsRecord))]
public record SuiRecord;
