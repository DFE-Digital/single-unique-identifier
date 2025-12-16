using System;
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
            .Where(p => p.Name != "EqualityContract" && p.Name != "AdditionalProperties")
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
                $"    public ConsolidatedField<{Type(p)}> {p.Name} {{ get; set; }} = ConsolidatedField<{Type(p)}>.Empty;"
            )
        );

        var propAssignments = string.Join(
            TwoNewLines,
            Properties.Select(p =>
            {
                var type = Type(p);
                return $"""
                            {p.Name} = new ConsolidatedField<{type}>(unconsolidatedRecords
                                .Select(r => new ConsolidatedFieldValue<{type}>(r.Record.{p.Name}, r.ProviderSystemId))
                                .OrderBy(x => {GenerateNullOrEmptyClause(p)} ? 1 : 0)
                                .ThenBy(x => rankField(x.ProviderSystemId, "{Name}", "{p.Name}", "{Name}.{p.Name}"))
                                .ThenBy(x => x.ProviderSystemId)
                                .ToArray()),
                """;
            })
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

        static bool IsStringProperty(IPropertySymbol prop) => prop.Type.Name == nameof(String);

        static bool IsEnumerableProperty(IPropertySymbol prop) =>
            prop.Type.AllInterfaces.Any(i =>
                i.OriginalDefinition.ToString() == "System.Collections.Generic.IEnumerable<T>"
            );

        static string GenerateNullOrEmptyClause(IPropertySymbol prop) =>
            prop switch
            {
                _ when IsStringProperty(prop) => "string.IsNullOrWhiteSpace(x.Value)",
                _ when IsEnumerableProperty(prop) => "x.Value == null || !x.Value.Any()",
                _ => "object.Equals(x.Value, null)",
            };

        // Note: we enforce all properties to be nullable on record types, because custodians may not always have all fields
        static string Type(IPropertySymbol prop) =>
            prop.Type.NullableAnnotation != NullableAnnotation.Annotated
                ? $"{prop.Type}?"
                : prop.Type.ToString();
    }
}
