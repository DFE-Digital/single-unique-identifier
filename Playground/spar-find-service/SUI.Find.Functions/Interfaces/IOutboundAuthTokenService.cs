using Interfaces;
using Models;

namespace SUI.Find.Functions.Interfaces
{
    public interface IOutboundAuthTokenService
    {
        Task<string?> GetAccessTokenAsync(AuthDefinition auth, CancellationToken ct);
    }
}