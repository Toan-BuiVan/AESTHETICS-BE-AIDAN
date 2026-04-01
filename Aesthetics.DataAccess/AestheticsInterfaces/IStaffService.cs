using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Aesthetics.Entities.Models.RequestModel.RequestUpdateStaff;

namespace Aesthetics.Data.AestheticsInterfaces
{
	public interface IStaffService
	{
		Task<BaseDataCollection<StaffResponseModel>> GetListAsync(RequestStaffSearch searchRequest);
		Task<bool> UpdateStaff(UpdateStaffRequest request);
	}
}
