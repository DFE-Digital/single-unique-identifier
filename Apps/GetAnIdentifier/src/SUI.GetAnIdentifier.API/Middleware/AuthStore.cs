using SUI.GetAnIdentifier.Infrastructure.Models;

namespace SUI.GetAnIdentifier.API.Middleware;

public class AuthStore
{
    public List<AuthClient>? Clients { get; set; }
}
