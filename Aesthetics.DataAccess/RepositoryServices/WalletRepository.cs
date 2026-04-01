using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices.Common;
using Aesthetics.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.RepositoryServices
{
    public class WalletRepository : CommonRepository<WalletEntity>, IWalletRepository
	{
		public WalletRepository(ILogger<CommonRepository<WalletEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}

		public async Task<bool> GetWalletById(int voucherId, int Customer)
		{
			var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.VoucherId == voucherId && x.CustomerId == Customer);
			if (wallet != null)
				return true;
			return false;
		}

		public async Task<ICollection<WalletEntity>> GetWalletsByPredicateWithVoucherAsync(Expression<Func<WalletEntity, bool>> predicate)
		{
			try
			{
				var wallets = await _dbContext.Set<WalletEntity>()
					.AsNoTracking()
					.Where(predicate)
					.Include(w => w.Voucher)
					.ToListAsync();

				_logger.LogInformation("GetWalletsByPredicateWithVoucherAsync - Count: {Count}", wallets.Count);
				return wallets;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetWalletsByPredicateWithVoucherAsync - Exception: {E}", ex);
				return [];
			}
		}
	}
}
