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
    public interface IAppointmentService
    {
		Task<bool> create(CreateAppointment appointment);

		Task<bool> delete(DeleteAppointment appointment);

		Task<BaseDataCollection<AppointmentEntity>> getlist(AppointmentGet appointment);
	}
}
