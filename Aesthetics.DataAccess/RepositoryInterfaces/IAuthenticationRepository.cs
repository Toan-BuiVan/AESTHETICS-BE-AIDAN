using Aesthetics.Data.RepositoryInterfaces.Common;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XAct.Users;

namespace Aesthetics.Data.RepositoryInterfaces
{
    public interface IAuthenticationRepository : ICommonRepository<AccountSessionEntity>
	{
		Task<AccountEntity?> login(RequestLogin request);
		Task<int> UpdateRefeshToken(int id, string RefeshToken, DateTime RefeshTokenExpiryTime);
		Task<AccountEntity> GetUserByUserName(string UserName);
		Task<bool> DeleteAccountSession(string? token, int? userID);
		Task<bool> DeleteAccountSessionAll(int? userID);
	}
}
