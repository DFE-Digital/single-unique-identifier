using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static SUI.Transfer.Domain.Generator.Consts;

namespace SUI.Transfer.Domain.Generator;

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
        $"    {ConsolidatedName} ConsolidateRecords(IUnconsolidatedRecord<{Name}>[] unconsolidatedRecords, FieldRanker rankField);";

    public string GenerateClassMethodSource() =>
        $"    public {ConsolidatedName} ConsolidateRecords(IUnconsolidatedRecord<{Name}>[] unconsolidatedRecords, FieldRanker rankField) => {ConsolidatedName}.FromUnconsolidated(unconsolidatedRecords, rankField);";

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
                                .OrderBy(x => rankField(x.ProviderSystemId, "{Name}", "{p.Name}", "{Name}.{p.Name}"))
                                .ThenBy(x => x.ProviderSystemId)
                                .ToArray()),
                """
            )
        );

        return $$"""
            public record {{ConsolidatedName}}
            {
            {{props}}

                public static {{ConsolidatedName}} FromUnconsolidated(IUnconsolidatedRecord<{{Name}}>[] unconsolidatedRecords, FieldRanker rankField)
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
