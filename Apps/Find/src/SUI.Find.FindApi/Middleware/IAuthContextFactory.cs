using System.IdentityModel.Tokens.Jwt;
using SUI.Find.FindApi.Models;

namespace SUI.Find.FindApi.Middleware;

public interface IAuthContextFactory
{
    AuthContext FromJwt(JwtSecurityToken jwt, bool useAuthStoreForAuthorisation);
}
