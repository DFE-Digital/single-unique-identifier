using NSubstitute;
using SUI.Transfer.Domain.Generator.Tests.ExampleModels;
using SUI.Transfer.Domain.Generator.Tests.MoreExampleModels;
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

    [Fact]
    public void GeneratedCode_Does_Consolidate_WhenThereIsASingleInputRecord()
    {
        // ACT
        var result = Sut.ConsolidateRecords(
            [
                new TestProviderRecord<ExampleRecord1>(
                    "Provider1",
                    new ExampleRecord1
                    {
                        FirstName = "ExampleFirstName",
                        LastName = "ExampleLastName",
                        DateOfBirth = new DateTimeOffset(2020, 6, 20, 0, 0, 0, TimeSpan.Zero),
                    }
                ),
            ],
            Substitute.For<FieldRanker>()
        );

        // ASSERT
        result.FirstName.Value.Should().Be("ExampleFirstName");
        result.FirstName.Values.Should().HaveCount(1);

        result.LastName.Value.Should().Be("ExampleLastName");
        result.LastName.Values.Should().HaveCount(1);

        result
            .DateOfBirth.Value.Should()
            .Be(new DateTimeOffset(2020, 6, 20, 0, 0, 0, TimeSpan.Zero));
        result.DateOfBirth.Values.Should().HaveCount(1);
    }

    [Fact]
    public void GeneratedCode_Does_Consolidate_WhenThereIsASingleInputRecord_WithNullValues()
    {
        // ACT
        var result = Sut.ConsolidateRecords(
            [new TestProviderRecord<ExampleRecord1>("Provider1", new ExampleRecord1())],
            Substitute.For<FieldRanker>()
        );

        // ASSERT
        result.FirstName.Value.Should().BeNull();
        result.FirstName.Values.Should().HaveCount(1);

        result.LastName.Value.Should().BeNull();
        result.LastName.Values.Should().HaveCount(1);

        result.DateOfBirth.Value.Should().BeNull();
        result.DateOfBirth.Values.Should().HaveCount(1);
    }

    [Fact]
    public void GeneratedCode_Does_Consolidate_WhenThereAreMultipleInputRecords()
    {
        // ACT
        var result = Sut.ConsolidateRecords(
            [
                new TestProviderRecord<ExampleRecord2>(
                    "Provider1",
                    new ExampleRecord2
                    {
                        Name = "Provider1Name",
                        Number = 1,
                        FlagA = true,
                        FlagB = true,
                    }
                ),
                new TestProviderRecord<ExampleRecord2>(
                    "Provider2",
                    new ExampleRecord2
                    {
                        Name = "Provider2Name",
                        Number = 2,
                        FlagA = false,
                        FlagB = true,
                    }
                ),
                new TestProviderRecord<ExampleRecord2>(
                    "Provider3",
                    new ExampleRecord2
                    {
                        Name = "Provider3Name",
                        Number = 3,
                        FlagA = false,
                        FlagB = false,
                    }
                ),
            ],
            Substitute.For<FieldRanker>()
        );

        // ASSERT
        result
            .Should()
            .BeEquivalentTo(
                new
                {
                    Name = new
                    {
                        Value = "Provider1Name",
                        Values = new[]
                        {
                            new { ProviderSystemId = "Provider1", Value = "Provider1Name" },
                            new { ProviderSystemId = "Provider2", Value = "Provider2Name" },
                            new { ProviderSystemId = "Provider3", Value = "Provider3Name" },
                        },
                    },
                    Number = new
                    {
                        Value = 1,
                        Values = new[]
                        {
                            new { ProviderSystemId = "Provider1", Value = 1 },
                            new { ProviderSystemId = "Provider2", Value = 2 },
                            new { ProviderSystemId = "Provider3", Value = 3 },
                        },
                    },
                    FlagA = new
                    {
                        Value = true,
                        Values = new[]
                        {
                            new { ProviderSystemId = "Provider1", Value = true },
                            new { ProviderSystemId = "Provider2", Value = false },
                            new { ProviderSystemId = "Provider3", Value = false },
                        },
                    },
                    FlagB = new
                    {
                        Value = true,
                        Values = new[]
                        {
                            new { ProviderSystemId = "Provider1", Value = true },
                            new { ProviderSystemId = "Provider2", Value = true },
                            new { ProviderSystemId = "Provider3", Value = false },
                        },
                    },
                },
                options => options.WithStrictOrdering()
            );
    }

    [Fact]
    public void GeneratedCode_Does_Support_ConsolidationOfRecordsWith_CollectionProperties()
    {
        var provider1Strings = new[] { "a", "z", "f" };
        var provider2Strings = new[] { "foo", "bar" };

        var provider1Nums = new[] { 8, 1, 4 };
        var provider2Nums = new[] { 6, 7 };

        var provider1DtoList = new[] { new ExampleDto("a", true), new ExampleDto("b", false) };
        var provider2DtoList = new[] { new ExampleDto("z", false) };

        // ACT
        var result = Sut.ConsolidateRecords(
            [
                new TestProviderRecord<ExampleRecord3>(
                    "Provider1",
                    new ExampleRecord3
                    {
                        ExampleStringList = provider1Strings,
                        ExampleNumberList = provider1Nums,
                        ExampleDtoList = provider1DtoList,
                    }
                ),
                new TestProviderRecord<ExampleRecord3>(
                    "Provider2",
                    new ExampleRecord3
                    {
                        ExampleStringList = provider2Strings,
                        ExampleNumberList = provider2Nums,
                        ExampleDtoList = provider2DtoList,
                    }
                ),
            ],
            Substitute.For<FieldRanker>()
        );

        // ASSERT
        result
            .Should()
            .BeEquivalentTo(
                new
                {
                    ExampleStringList = new
                    {
                        Value = provider1Strings,
                        Values = new[]
                        {
                            new { ProviderSystemId = "Provider1", Value = provider1Strings },
                            new { ProviderSystemId = "Provider2", Value = provider2Strings },
                        },
                    },
                    ExampleNumberList = new
                    {
                        Value = provider1Nums,
                        Values = new[]
                        {
                            new { ProviderSystemId = "Provider1", Value = provider1Nums },
                            new { ProviderSystemId = "Provider2", Value = provider2Nums },
                        },
                    },
                    ExampleDtoList = new
                    {
                        Value = provider1DtoList,
                        Values = new[]
                        {
                            new { ProviderSystemId = "Provider1", Value = provider1DtoList },
                            new { ProviderSystemId = "Provider2", Value = provider2DtoList },
                        },
                    },
                },
                options => options.WithStrictOrdering()
            );
    }

    [Fact]
    public void GeneratedCode_Does_Order_Null_Values_ToTheBack()
    {
        // ACT
        var result = Sut.ConsolidateRecords(
            [
                new TestProviderRecord<ExampleRecord2>(
                    "Provider1",
                    new ExampleRecord2
                    {
                        Name = null,
                        Number = 1,
                        FlagA = null,
                    }
                ),
                new TestProviderRecord<ExampleRecord2>(
                    "Provider2",
                    new ExampleRecord2
                    {
                        Name = "Name2",
                        Number = null,
                        FlagA = null,
                    }
                ),
                new TestProviderRecord<ExampleRecord2>(
                    "Provider3",
                    new ExampleRecord2
                    {
                        Name = "Name3",
                        Number = 3,
                        FlagA = true,
                    }
                ),
            ],
            Substitute.For<FieldRanker>()
        );

        // ASSERT
        result
            .Should()
            .BeEquivalentTo(
                new
                {
                    Name = new
                    {
                        Value = "Name2",
                        Values = new[]
                        {
                            new { ProviderSystemId = "Provider2", Value = (string?)"Name2" },
                            new { ProviderSystemId = "Provider3", Value = (string?)"Name3" },
                            new { ProviderSystemId = "Provider1", Value = (string?)null },
                        },
                    },
                    Number = new
                    {
                        Value = 1,
                        Values = new[]
                        {
                            new { ProviderSystemId = "Provider1", Value = (int?)1 },
                            new { ProviderSystemId = "Provider3", Value = (int?)3 },
                            new { ProviderSystemId = "Provider2", Value = (int?)null },
                        },
                    },
                    FlagA = new
                    {
                        Value = true,
                        Values = new[]
                        {
                            new { ProviderSystemId = "Provider3", Value = (bool?)true },
                            new { ProviderSystemId = "Provider1", Value = (bool?)null },
                            new { ProviderSystemId = "Provider2", Value = (bool?)null },
                        },
                    },
                },
                options => options.WithStrictOrdering()
            );
    }

    [Fact]
    public void GeneratedCode_Does_Order_EmptyOrWhitespaceStrings_ToTheBack()
    {
        // ACT
        var result = Sut.ConsolidateRecords(
            [
                new TestProviderRecord<ExampleRecord2>(
                    "Provider1",
                    new ExampleRecord2 { Name = "" }
                ),
                new TestProviderRecord<ExampleRecord2>(
                    "Provider2",
                    new ExampleRecord2 { Name = "  \t\t  " }
                ),
                new TestProviderRecord<ExampleRecord2>(
                    "Provider3",
                    new ExampleRecord2 { Name = "Name3" }
                ),
            ],
            Substitute.For<FieldRanker>()
        );

        // ASSERT
        result
            .Should()
            .BeEquivalentTo(
                new
                {
                    Name = new
                    {
                        Value = "Name3",
                        Values = new[]
                        {
                            new { ProviderSystemId = "Provider3", Value = "Name3" },
                            new { ProviderSystemId = "Provider1", Value = "" },
                            new { ProviderSystemId = "Provider2", Value = "  \t\t  " },
                        },
                    },
                },
                options => options.WithStrictOrdering()
            );
    }

    // rs-todo: does order by provider name

    // rs-todo: does order by FieldRanker then provider name then null values
}
