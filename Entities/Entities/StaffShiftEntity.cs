using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("StaffShifts")]
	public class StaffShiftEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Staffs: nhân viên/bác sĩ</summary>
		public int? StaffId { get; set; }

		/// <summary>Ngày của ca làm việc</summary>
		[Column(TypeName = "date")]
		public DateTime? Date { get; set; }

		/// <summary>false = Đã phân, true = Hoàn thành</summary>
		public bool? Status { get; set; }

		/// <summary>false = Đã phân, true = Hoàn thành</summary>
		public bool? ShiftType { get; set; }

		// Navigation properties
		[ForeignKey(nameof(StaffId))]
		public virtual StaffEntity? Staff { get; set; }
	}
}
