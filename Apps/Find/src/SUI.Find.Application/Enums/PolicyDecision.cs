namespace SUI.Find.Application.Enums;

public enum PolicyDecision
{
    Allowed,
    Denied,
    Indeterminate, // Policy could not be evaluated (System/Network/Read Error)
}
