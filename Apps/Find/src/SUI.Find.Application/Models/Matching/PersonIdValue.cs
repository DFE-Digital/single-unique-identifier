namespace SUI.Find.Application.Models.Matching;

public abstract record PersonIdValue(string Value);

public sealed record PlainPersonId(string Value) : PersonIdValue(Value);

public sealed record EncryptedSuidPersonId(string Value) : PersonIdValue(Value);
