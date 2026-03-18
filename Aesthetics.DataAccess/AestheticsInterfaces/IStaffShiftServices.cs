using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
    public interface IStaffShiftServices 
    {
		Task<bool> create(CreateStaffShift staffShift);

		Task<bool> delete(DeleteStaffShift staffShift);

		Task<BaseDataCollection<StaffShiftEntity>> getlist(GetStaffShift staffShift);
	}
}
