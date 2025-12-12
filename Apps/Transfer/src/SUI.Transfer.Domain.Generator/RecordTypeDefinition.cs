using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using static SUI.Transfer.Domain.Generator.Consts;

namespace SUI.Transfer.Domain.Generator;

[ExcludeFromCodeCoverage] // This is thoroughly behaviourally tested by the `RecordConsolidationSourceGeneratorTests`, it just isn't picked up by coverage tools.
public class RecordTypeDefinition(INamedTypeSymbol symbol)
{
    public INamedTypeSymbol Symbol { get; } = symbol;

    public string Name { get; } = symbol.Name;

    public string ConsolidatedName { get; } = $"{symbol.Name}Consolidated";

    public string Namespace { get; } = symbol.ContainingNamespace.ToDisplayString();

    public IReadOnlyList<IPropertySymbol> Properties { get; } =
        symbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.Name != "EqualityContract")
            .ToArray();

    public string GenerateInterfaceMethodSource() =>
        $"    {ConsolidatedName} ConsolidateRecords(IProviderRecord<{Name}>[] unconsolidatedRecords, FieldRanker rankField);";

    public string GenerateClassMethodSource() =>
        $"    public {ConsolidatedName} ConsolidateRecords(IProviderRecord<{Name}>[] unconsolidatedRecords, FieldRanker rankField) => {ConsolidatedName}.FromUnconsolidated(unconsolidatedRecords, rankField);";

    public string GenerateConsolidateClassSource()
    {
        var props = string.Join(
            TwoNewLines,
            Properties.Select(p =>
                $"    public required ConsolidatedField<{p.Type}> {p.Name} {{ get; init; }}"
            )
        );

        var propAssignments = string.Join(
            TwoNewLines,
            Properties.Select(p =>
                $"""
                            {p.Name} = new ConsolidatedField<{p.Type}>(unconsolidatedRecords
                                .Select(r => new ConsolidatedFieldValue<{p.Type}>(r.Record.{p.Name}, r.ProviderSystemId))
                                .OrderBy(x => object.Equals(x.Value, null) || (x.Value as object is string str && string.IsNullOrWhiteSpace(str)) ? 1 : 0)
                                .ThenBy(x => rankField(x.ProviderSystemId, "{Name}", "{p.Name}", "{Name}.{p.Name}"))
                                .ThenBy(x => x.ProviderSystemId)
                                .ToArray()),
                """
            )
        );

        return $$"""
            {{ExcludeFromCodeCoverageAttributeSource}}
            public record {{ConsolidatedName}}
            {
            {{props}}

                public static {{ConsolidatedName}} FromUnconsolidated(IProviderRecord<{{Name}}>[] unconsolidatedRecords, FieldRanker rankField)
                {
                    return new {{ConsolidatedName}}
                    {
            {{propAssignments}}
                    };
                }
            }
            """;
    }
}
