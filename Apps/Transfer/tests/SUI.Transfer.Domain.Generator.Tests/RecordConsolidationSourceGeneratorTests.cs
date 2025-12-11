using NSubstitute;
using SUI.Transfer.Domain.Generator.Tests.ExampleModels;
using SUI.Transfer.Domain.SourceGenerated;

namespace SUI.Transfer.Domain.Generator.Tests;

public class RecordConsolidationSourceGeneratorTests
{
    private static readonly ExampleConsolidateRecordCollections Sut = new();

    [Fact]
    public void GeneratedCode_DoesSupport_Consolidation_WhenThereAreZeroInputRecords()
    {
        // ACT
        var result = Sut.ConsolidateRecords(
            Array.Empty<IProviderRecord<ExampleRecord1>>(),
            Substitute.For<FieldRanker>()
        );

        // ASSERT
        result.FirstName.Value.Should().BeNull();
        result.FirstName.Values.Should().HaveCount(0);

        result.LastName.Value.Should().BeNull();
        result.LastName.Values.Should().HaveCount(0);

        result.DateOfBirth.Value.Should().BeNull();
        result.DateOfBirth.Values.Should().HaveCount(0);
    }
}
