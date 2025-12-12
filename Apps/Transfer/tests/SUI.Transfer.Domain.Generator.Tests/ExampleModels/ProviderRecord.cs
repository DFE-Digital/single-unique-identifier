using SUI.Transfer.Domain.SourceGenerated;

namespace SUI.Transfer.Domain.Generator.Tests.ExampleModels;

public record TestProviderRecord<TRecord>(string ProviderSystemId, TRecord Record)
    : IProviderRecord<TRecord> { }
