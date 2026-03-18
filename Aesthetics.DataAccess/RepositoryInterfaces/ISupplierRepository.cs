using Aesthetics.Data.RepositoryInterfaces.Common;
using Aesthetics.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.RepositoryInterfaces
{
    public interface ISupplierRepository : ICommonRepository<SupplierEntity>
	{
        Task<SupplierEntity?> GetByName(string name);
		Task<bool> delete(int supplierId);
	}
}
