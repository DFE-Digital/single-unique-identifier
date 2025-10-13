namespace SUI.Find.Domain.ValueObjects;

public record Gender
{
    public string Value { get; }

    private Gender(string value) => Value = value;

    // Predefined domain values
    public static readonly Gender Male = new("male");
    public static readonly Gender Female = new("female");
    public static readonly Gender NotKnown = new("not known");
    public static readonly Gender NotSpecified = new("not specified");
    public static readonly Gender Unknown = new("unknown");
    
    public override string ToString() => Value;
    
}