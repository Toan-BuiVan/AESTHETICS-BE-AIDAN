using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("AppointmentTimeLocks")]
	public class AppointmentTimeLockEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		public int? ClinicId { get; set; }

		public DateTime? StartTime { get; set; }

		public DateTime? EndTime { get; set; }

		public bool? IsOverloaded { get; set; }

		public int? CreatedBy { get; set; }

		public DateTime? CreationDate { get; set; }

		// Navigation property
		public ClinicEntity? Clinic { get; set; }
	}
}
