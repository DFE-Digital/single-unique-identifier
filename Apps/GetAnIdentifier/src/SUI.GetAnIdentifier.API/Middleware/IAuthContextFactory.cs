using System.IdentityModel.Tokens.Jwt;
using SUI.GetAnIdentifier.API.Models;

namespace SUI.GetAnIdentifier.API.Middleware;

public interface IAuthContextFactory
{
    AuthResult FromJwt(JwtSecurityToken jwt, bool useAuthStoreForAuthorisation);
}
