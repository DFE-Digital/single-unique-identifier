namespace SUI.Transfer.Domain;

public record ProviderRecord<TRecord>(string ProviderSystemId, TRecord Record) { }
