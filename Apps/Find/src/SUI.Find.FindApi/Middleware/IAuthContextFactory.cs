using System.IdentityModel.Tokens.Jwt;
using SUI.Find.FindApi.Models;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.FindApi.Middleware;

public interface IAuthContextFactory
{
    AuthContext FromJwt(JwtSecurityToken jwt, AuthStore store);
}
