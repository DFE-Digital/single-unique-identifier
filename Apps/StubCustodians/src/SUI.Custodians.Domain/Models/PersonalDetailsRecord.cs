namespace SUI.Custodians.Domain.Models;

/// <summary>
/// General personal details about a specific child.
/// </summary>
public record PersonalDetailsRecord : SuiRecord
{
    /// <summary>
    /// The child's first name.
    /// </summary>
    /// <example>Sarah</example>
    public string? FirstName { get; init; }

    /// <summary>
    /// The child's last name.
    /// </summary>
    /// <example>Smith</example>
    public string? LastName { get; init; }

    /// <summary>
    /// The child's date of birth.
    /// </summary>
    /// <example>2011-09-25</example>
    public DateOnly? DateOfBirth { get; init; }

    /// <summary>
    /// The latest known address of the child's main residence.
    /// </summary>
    /// <example>12 Burton Street, London, SW1A 0AA</example>
    public Address? Address { get; init; }

    /// <summary>
    /// The full names of the other people known to be residing at the child's main address.
    /// </summary>
    /// <example>James Smith, Henry Smith, Thomas Smith, Jason Archer, Sarah Flint-Smith</example>
    public IReadOnlyCollection<string>? NamesOfIndividualsResidingAtMainAddress { get; init; }

    /// <summary>
    /// Birth assigned sex
    /// </summary>
    /// <example>Female</example>
    public string? BirthAssignedSex { get; init; }

    /// <summary>
    /// Pronouns
    /// </summary>
    /// <example>She/her</example>
    public string? Pronouns { get; init; }

    /// <summary>
    /// Ethnicity
    /// </summary>
    /// <example>Irish Traveller</example>
    public string? Ethnicity { get; init; }

    /// <summary>
    /// First language
    /// </summary>
    /// <example>English</example>
    public string? FirstLanguage { get; init; }

    /// <summary>
    /// Designated Local Authority
    /// </summary>
    /// <example>Bromley</example>
    public string? DesignatedLocalAuthority { get; init; }

    /// <summary>
    /// Communication need: English as additional language (EAL)
    /// </summary>
    public bool? EnglishAsAdditionalLanguage { get; init; }

    /// <summary>
    /// Communication need: Braille needed
    /// </summary>
    public bool? Braille { get; init; }

    /// <summary>
    /// Communication need: Sign language
    /// </summary>
    public bool? SignLanguage { get; init; }

    /// <summary>
    /// Communication need: Makaton needed
    /// </summary>
    public bool? Makaton { get; init; }

    /// <summary>
    /// Communication need: Interpreter needed
    /// </summary>
    public bool? Interpreter { get; init; }

    /// <summary>
    /// The people known to be related to the child.
    /// </summary>
    public IReadOnlyCollection<RelatedPerson>? RelatedPeople { get; init; }
}
