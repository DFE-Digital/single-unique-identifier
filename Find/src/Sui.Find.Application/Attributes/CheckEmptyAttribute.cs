namespace SUI.Find.Application.Attributes;

/// <summary>
/// Simple attribute used to mark properties that should be checked for empty values.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CheckEmptyAttribute : Attribute
{
}