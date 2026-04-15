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
	public class CustomerPaymentInfoRepository : CommonRepository<CustomerPaymentInfoEntity>, ICustomerPaymentInfoRepository
	{
		public CustomerPaymentInfoRepository(ILogger<CommonRepository<CustomerPaymentInfoEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}
	}
}
