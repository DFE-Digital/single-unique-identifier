using SUI.Custodians.API.Client;
using SUI.Transfer.Domain.SourceGenerated;

namespace SUI.Transfer.Domain.Services;

[GenerateRecordConsolidation(
    typeof(PersonalDetailsRecordV1),
    typeof(ChildrensServicesDetailsRecordV1),
    typeof(EducationDetailsRecordV1),
    typeof(HealthDataRecordV1),
    typeof(CrimeDataRecordV1)
)]
public partial class ConsolidateRecordCollectionsService { }
