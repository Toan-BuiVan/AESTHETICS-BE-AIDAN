using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices.Common;
using Aesthetics.Entities.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.RepositoryServices
{
    public class SessionProductRepository : CommonRepository<SessionProductEntity>, ISessionProductRepository
	{
		public SessionProductRepository(ILogger<CommonRepository<SessionProductEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}
	}
}
