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
    public class CartRepository : CommonRepository<CartEntity>, ICartRepository
	{
		public CartRepository(ILogger<CommonRepository<CartEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}
	}
}
