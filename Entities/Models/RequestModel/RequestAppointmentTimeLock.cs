using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class CreateAppointmentTimeLock
    {
		public int? StaffId { get; set; }

		public DateTime? StartTime { get; set; }

		public DateTime? EndTime { get; set; }

		public bool? IsOverloaded { get; set; }
	}

	public class UpdateAppointmentTimeLock
	{
		public int? Id { get; set; }

		public DateTime? StartTime { get; set; }

		public DateTime? EndTime { get; set; }

		public bool? IsOverloaded { get; set; }

	}

	public class DeleteAppointmentTimeLock
	{
		public int? Id { get; set; }
	}

	public class GetAppointmentTimeLock : BaseSearchModel
	{
		public int? ClinicId { get; set; }
		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public bool? IsOverloaded { get; set; }
	}
}
