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
    public class ClinicStaffRepository : CommonRepository<ClinicStaffEntity>, IClinicStaffRepository
	{
		public ClinicStaffRepository(ILogger<CommonRepository<ClinicStaffEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}

		public async Task<bool> CheckStaffId(int id)
		{
			var isStaffExist = await _dbContext.ClinicStaffs
					.AnyAsync(s => s.StaffId == id && !s.DeleteStatus);
			if (isStaffExist)
			{
				return true;
			}
			return false;
		}
	}
}
