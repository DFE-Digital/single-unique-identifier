using System.Text.Json.Serialization;

namespace SUI.Custodians.Domain.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(
    typeof(ChildrensServicesDetailsRecordV1),
    typeDiscriminator: nameof(ChildrensServicesDetailsRecordV1)
)]
[JsonDerivedType(typeof(CrimeDataRecordV1), typeDiscriminator: nameof(CrimeDataRecordV1))]
[JsonDerivedType(
    typeof(EducationDetailsRecordV1),
    typeDiscriminator: nameof(EducationDetailsRecordV1)
)]
[JsonDerivedType(typeof(HealthDataRecordV1), typeDiscriminator: nameof(HealthDataRecordV1))]
[JsonDerivedType(
    typeof(PersonalDetailsRecordV1),
    typeDiscriminator: nameof(PersonalDetailsRecordV1)
)]
public record SuiRecord;
