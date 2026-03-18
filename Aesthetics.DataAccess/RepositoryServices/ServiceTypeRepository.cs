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

namespace Aesthetics.Data.RepositoryServices
{
    public class ServiceTypeRepository : CommonRepository<ServiceTypeEntity>, IServiceTypeRepository
	{
		public ServiceTypeRepository(ILogger<CommonRepository<ServiceTypeEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}

		public async Task<ServiceTypeEntity?> GetByName(string name)
		{
			try
			{
				var serviceType = await _dbContext.ServiceTypes
					.AsNoTracking()
					.FirstOrDefaultAsync(x => x.ServiceTypeName.ToLower() == name.ToLower());
				return serviceType;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetByName Exception: SupplierName '{SupplierName}'", name);
				return null;
			}
		}
	}
}
