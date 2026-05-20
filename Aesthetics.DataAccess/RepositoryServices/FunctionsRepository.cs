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
	public class FunctionsRepository : CommonRepository<FunctionEntity>, IFunctionsRepository
	{
		public FunctionsRepository(ILogger<CommonRepository<FunctionEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}
		public async Task<List<FunctionEntity>> FindFunctionsByCodesAsync(string[] functionCodes)
		{
			var result = await FindByPredicate(f => functionCodes.Contains(f.FunctionCode ?? "") && !f.DeleteStatus);
			return result.ToList();
		}
	}
}
