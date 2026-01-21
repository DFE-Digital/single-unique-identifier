namespace SUI.Custodians.Domain.Models;

/// <summary>
/// Represents a person related to a specific child.
/// </summary>
public record RelatedPerson
{
    /// <summary>
    /// Related Person - Relationship to the child
    /// </summary>
    /// <example>Father</example>
    public string? RelationshipToTheChild { get; init; }

    /// <summary>
    /// Related Person - Full Name
    /// </summary>
    /// <example>James Smith</example>
    public string? Name { get; init; }

    /// <summary>
    /// Related Person - Date of birth
    /// </summary>
    /// <example>1978-11-01</example>
    public DateOnly? DOB { get; init; }

    /// <summary>
    /// Risks posed by the Related Person
    /// </summary>
    /// <example>Individual may possess firearms</example>
    public IReadOnlyCollection<string>? Risk { get; init; }
}
