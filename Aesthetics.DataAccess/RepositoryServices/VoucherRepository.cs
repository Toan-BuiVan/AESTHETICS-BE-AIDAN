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
	public class VoucherRepository : CommonRepository<VoucherEntity>, IVoucherRepository
	{
		public VoucherRepository(ILogger<CommonRepository<VoucherEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}

		public async Task<string> GenCodeUnique()
		{
			string code;
			bool exists;
			Random random = new Random();
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

			do
			{
				code = new string(chars.OrderBy(x => random.Next()).Take(5).ToArray());
				exists = await _dbContext.Vouchers.AnyAsync(s => s.Code == code);
			} while (exists);
			return code;
		}
	}
}
