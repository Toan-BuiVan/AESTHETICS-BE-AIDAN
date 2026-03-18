using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces.TokenService
{
    public interface ITokenService
    {
		Task<string> GetDeviceName();
		Task<string> GenerateRefreshToken();
		Task<JwtSecurityToken> CreateToken(List<Claim> authClaims);
		Task<ClaimsPrincipal?> GetPrincipalFromExpiredToken(string? token);	}
}
