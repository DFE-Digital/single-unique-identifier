namespace SUI.Find.Application.Enums;

public enum PolicyDecision
{
    Allowed,

    Denied,

    /// <summary>
    /// Policy could not be evaluated (System/Network/Read Error)
    /// </summary>
    Indeterminate,
}
