using Aesthetics.Data.AestheticsInterfaces.TokenService;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices.TokenService
{
	public class TokenService : ITokenService
	{
		private Microsoft.Extensions.Configuration.IConfiguration _configuration;
		public TokenService(Microsoft.Extensions.Configuration.IConfiguration configuration)
		{
			_configuration = configuration;
		}
		public async Task<JwtSecurityToken> CreateToken(List<Claim> authClaims)
		{
			if (authClaims == null)
			{
				throw new ArgumentNullException(nameof(authClaims), "authClaims cannot be null");
			}
			var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
			_ = int.TryParse(_configuration["JWT:TokenValidityInMinutes"], out int tokenValidityInMinutes);

			var token = new JwtSecurityToken(
				issuer: _configuration["JWT:ValidIssuer"],
				audience: _configuration["JWT:ValidAudience"],
				expires: DateTime.Now.AddMinutes(tokenValidityInMinutes),
				claims: authClaims,
				signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
			);

			return token;
		}

		public async Task<string> GenerateRefreshToken()
		{
			var randomNumber = new byte[64];
			using var rng = RandomNumberGenerator.Create();
			rng.GetBytes(randomNumber);
			return Convert.ToBase64String(randomNumber);
		}

		public async Task<string> GetDeviceName()
		{
			return Environment.MachineName;
		}

		public async Task<ClaimsPrincipal?> GetPrincipalFromExpiredToken(string? token)
		{
			var tokenValidationParameters = new TokenValidationParameters
			{
				ValidateAudience = false,
				ValidateIssuer = false,
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"])),
				ValidateLifetime = false
			};

			var tokenHandler = new JwtSecurityTokenHandler();
			var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
			if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
				throw new SecurityTokenException("Invalid token");

			return principal;
		}
	}
}
