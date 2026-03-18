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
    public interface IClinicService
    {
		Task<bool> create(RequestClinic clinic);

		Task<bool> update(UpdateClinic clinic);

		Task<bool> delete(DeleteClinic clinic);

		Task<BaseDataCollection<ClinicEntity>> getlist(ClinicGet clinic);
	}
}
