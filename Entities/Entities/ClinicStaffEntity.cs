using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("ClinicStaff")]
	public class ClinicStaffEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Clinics: phòng nào</summary>
		public int? ClinicId { get; set; }

		/// <summary>FK → Staffs: nhân viên/bác sĩ nào</summary>
		public int? StaffId { get; set; }

		// Navigation properties
		[ForeignKey(nameof(ClinicId))]
		public virtual ClinicEntity? Clinic { get; set; }

		[ForeignKey(nameof(StaffId))]
		public virtual StaffEntity? Staff { get; set; }
	}
}
