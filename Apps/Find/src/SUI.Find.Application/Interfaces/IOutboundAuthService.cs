using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;

namespace SUI.Find.Application.Interfaces;

public interface IOutboundAuthService
{
    Task<Result<string>> GetAccessTokenAsync(
        ProviderDefinition providerDefinition,
        CancellationToken cancellationToken
    );
}
