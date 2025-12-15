using SUI.Custodians.API.Client;

namespace SUI.Transfer.Domain.Consolidation;

[GenerateRecordConsolidation(
    typeof(PersonalDetailsRecordV1),
    typeof(ChildSocialCareDetailsRecordV1),
    typeof(EducationDetailsRecordV1),
    typeof(HealthDataRecordV1),
    typeof(CrimeDataRecordV1)
)]
public partial class ConsolidateRecordCollectionsService { }
