using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
	public interface IPermissionService
	{
		Task<BaseDataCollection<PermissionResponseModel>> GetPermissionListAsync(GetPermissionListRequest request);
		Task<bool> UpdatePermissionAsync(UpdatePermissionRequest request);
	}
}
