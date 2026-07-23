using System.IdentityModel.Tokens.Jwt;
using SUI.GetAnIdentifier.Function.Models;

namespace SUI.GetAnIdentifier.Function.Middleware;

public interface IAuthContextFactory
{
    AuthResult FromJwt(JwtSecurityToken jwt, bool useAuthStoreForAuthorisation);
}
