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
    public interface IClinicStaffService
    {
		Task<bool> create(CreateClinicStaff clinicStaff);

		Task<bool> update(UpdateClinicStaff clinicStaff);

		Task<bool> delete(DeleteClinicStaff clinicStaff);

		Task<BaseDataCollection<ClinicStaffEntity>> getlist(GetClinicStaff clinicStaff);
	}
}
