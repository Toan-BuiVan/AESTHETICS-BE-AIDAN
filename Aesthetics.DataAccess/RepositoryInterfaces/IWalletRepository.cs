using Aesthetics.Data.RepositoryInterfaces.Common;
using Aesthetics.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.RepositoryInterfaces
{
    public interface IWalletRepository : ICommonRepository<WalletEntity>
	{
        Task<bool> GetWalletById (int voucherId, int Customer);
		Task<ICollection<WalletEntity>> GetWalletsByPredicateWithVoucherAsync(Expression<Func<WalletEntity, bool>> predicate);
	}
}
