namespace SUI.Transfer.Application.Models.Custodians;

public class PoliceData : ICustodianRecord
{
    public bool ChildProtection { get; init; }

    public bool KnownToPolice { get; init; }

    public bool PolicePowersOfProtection { get; init; }
}
