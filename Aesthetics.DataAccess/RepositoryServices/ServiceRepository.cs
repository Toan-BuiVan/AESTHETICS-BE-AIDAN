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
    public class ServiceRepository : CommonRepository<ServiceEntity>, IServiceRepository
	{
		public ServiceRepository(ILogger<CommonRepository<ServiceEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}

		public async Task<ServiceEntity?> GetByName(string name)
		{
			try
			{
				var Service = await _dbContext.Services
					.AsNoTracking()
					.FirstOrDefaultAsync(x => x.ServiceName.ToLower() == name.ToLower());
				return Service;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetByName Exception: ProductName '{ProductName}'", name);
				return null;
			}
		}
	}
}
