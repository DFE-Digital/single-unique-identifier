namespace SUI.Find.Application.Models.Matching;

public abstract record PersonIdValue(string Value);

public sealed record PlainPersonId(string Value) : PersonIdValue(Value);
