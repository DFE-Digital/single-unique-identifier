using System.IdentityModel.Tokens.Jwt;
using SUI.Find.FindApi.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.Middleware;

public interface IAuthContextFactory
{
    AuthContext FromJwt(
        JwtSecurityToken jwt,
        IAuthStoreService storeService,
        bool useAuthStoreForAuthorisation
    );
}
