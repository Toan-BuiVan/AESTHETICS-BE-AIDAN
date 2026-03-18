using Aesthetics.Data.RepositoryInterfaces.Common;
using Aesthetics.Entities.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.RepositoryInterfaces
{
	public interface IAccountRepository : ICommonRepository<AccountEntity>
	{
		Task<AccountEntity?> GetByName(string name);
		Task<string> GenerateUniqueReferralCode();
		Task<FunctionEntity?> GetFunctionIDByName(string functionCode);
		Task<PermissionEntity?> GetPermisstionUserIDOfFunctionID(int userId, int functionId);
		Task<AccountEntity?> GetUser_ByUserName(string userName);
		Task<bool> UserUpdate_RefeshToken(int userId, string refreshToken, DateTime expiry);
		Task<AccountEntity?> User_GetByID(int id);
	}
}
