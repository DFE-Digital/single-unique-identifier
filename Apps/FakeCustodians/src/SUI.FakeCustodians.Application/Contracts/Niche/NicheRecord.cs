namespace SUI.FakeCustodians.Application.Contracts.Niche
{
    public class NicheRecord : BaseEntity
    {
        public bool ChildProtection { get; init; }

        public bool KnownToPolice { get; init; }

        public bool PolicePowersOfProtection { get; init; }
    }
}
