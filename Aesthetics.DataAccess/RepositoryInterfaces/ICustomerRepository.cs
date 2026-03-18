using Aesthetics.Data.RepositoryInterfaces.Common;
using Aesthetics.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XAct.Users;

namespace Aesthetics.Data.RepositoryInterfaces
{
    public interface ICustomerRepository : ICommonRepository<CustomerEntity>
	{
		Task<CustomerEntity> GetUserIdByReferralCode(string referralCode);
		Task UpdateAccumulatedPoints(int userId);
	}
}
