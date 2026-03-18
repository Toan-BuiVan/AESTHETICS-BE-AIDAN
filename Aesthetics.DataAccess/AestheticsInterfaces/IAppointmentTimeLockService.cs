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
    public interface IAppointmentTimeLockService
    {
		Task<bool> create(CreateAppointmentTimeLock timeLock);

		Task<bool> update(UpdateAppointmentTimeLock timeLock);

		Task<bool> delete(DeleteAppointmentTimeLock timeLock);

		Task<BaseDataCollection<AppointmentTimeLockEntity>> getlist(GetAppointmentTimeLock timeLock);
	}
}
