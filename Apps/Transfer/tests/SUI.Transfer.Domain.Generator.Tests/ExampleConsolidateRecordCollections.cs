using SUI.Transfer.Domain.Consolidation;
using SUI.Transfer.Domain.Generator.Tests.ExampleModels;
using SUI.Transfer.Domain.Generator.Tests.MoreExampleModels;

namespace SUI.Transfer.Domain.Generator.Tests;

[GenerateRecordConsolidation(
    typeof(ExampleRecord1),
    typeof(ExampleRecord2),
    typeof(ExampleRecord3)
)]
public partial class ExampleConsolidateRecordCollections { }
