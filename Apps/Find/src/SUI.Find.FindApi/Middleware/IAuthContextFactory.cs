using System.IdentityModel.Tokens.Jwt;
using SUI.Find.FindApi.Models.Auth;

namespace SUI.Find.FindApi.Middleware;

public interface IAuthContextFactory
{
    AuthResult FromJwt(JwtSecurityToken jwt, bool useAuthStoreForAuthorisation);
}
