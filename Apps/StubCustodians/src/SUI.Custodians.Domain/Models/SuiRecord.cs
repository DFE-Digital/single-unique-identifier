using System.Text.Json.Serialization;

namespace SUI.Custodians.Domain.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(
    typeof(ChildSocialCareDetailsRecordV1),
    typeDiscriminator: "ChildSocialCareDetailsRecordV1"
)]
[JsonDerivedType(typeof(CrimeDataRecordV1), typeDiscriminator: "CrimeDataRecordV1")]
[JsonDerivedType(typeof(EducationDetailsRecordV1), typeDiscriminator: "EducationDetailsRecordV1")]
[JsonDerivedType(typeof(HealthDataRecordV1), typeDiscriminator: "HealthDataRecordV1")]
[JsonDerivedType(typeof(PersonalDetailsRecordV1), typeDiscriminator: "PersonalDetailsRecordV1")]
public record SuiRecord;
