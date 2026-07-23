using SUI.GetAnIdentifier.Infrastructure.Models;

namespace SUI.GetAnIdentifier.Function.Middleware;

public class AuthStore
{
    public List<AuthClient>? Clients { get; set; }
}
