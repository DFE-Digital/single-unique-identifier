using Interfaces;
using Models;

public sealed class CallerEncryptionResolver : ICallerEncryptionResolver
{
    private readonly ICustodianRegistry _custodians;

    public CallerEncryptionResolver(ICustodianRegistry custodians)
    {
        _custodians = custodians;
    }

    public EncryptionDefinition ResolveForCaller(AuthContext auth)
    {
        if (auth is null || string.IsNullOrWhiteSpace(auth.ClientId))
        {
            throw new UnauthorizedAccessException("Authenticated organisation (org claim) is missing.");
        }

        var enc = _custodians
            .GetCustodians()
            .FirstOrDefault(p => string.Equals(p.OrgId, auth.ClientId, StringComparison.OrdinalIgnoreCase))
            ?.Encryption;

        if (enc is null)
        {
            throw new UnauthorizedAccessException($"No encryption configured for caller organisation '{auth.ClientId}'.");
        }

        return enc;
    }
}
