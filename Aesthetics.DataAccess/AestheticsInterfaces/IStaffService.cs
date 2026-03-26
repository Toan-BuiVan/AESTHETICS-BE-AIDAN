using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
	public interface IStaffService
	{
		Task<BaseDataCollection<StaffResponseModel>> GetListAsync(RequestStaffSearch searchRequest);
	}
}
