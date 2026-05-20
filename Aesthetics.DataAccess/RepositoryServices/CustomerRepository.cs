using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices.Common;
using Aesthetics.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XAct.Users;

namespace Aesthetics.Data.RepositoryServices
{
    public class CustomerRepository : CommonRepository<CustomerEntity>, ICustomerRepository
    {
		public CustomerRepository(ILogger<CommonRepository<CustomerEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{
		}

		public async Task<CustomerEntity> GetUserIdByReferralCode(string referralCode)
		{
			return await _dbContext.Customers.Where(s => s.ReferralCode == referralCode && s.DeleteStatus == false).FirstOrDefaultAsync();
		}

		public async Task UpdateAccumulatedPoints(int userId)
		{
			try
			{
				var user = await _dbContext.Customers.FindAsync(userId);
				if (user != null)
				{
					user.AccumulatedPoints += 10;
					await _dbContext.SaveChangesAsync();
				}
			}
			catch(Exception ex)
			{
				throw new Exception($"Lỗi: {ex}");
			}
		}
	}
}
