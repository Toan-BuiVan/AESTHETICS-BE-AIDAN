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

		/// <summary>0 = Đã phân, 1 = Hoàn thành, 2 = Nghỉ </summary>
		public int? Status { get; set; }

		/// <summary>Thời gian bắt đầu ca</summary>
		[Column(TypeName = "datetime")]
		public DateTime? StartDate { get; set; }

		/// <summary>Thời gian kết thúc ca</summary>
		[Column(TypeName = "datetime")]
		public DateTime? EndDate { get; set; }

		// Navigation properties
		[ForeignKey(nameof(StaffId))]
		public virtual StaffEntity? Staff { get; set; }
	}
}
