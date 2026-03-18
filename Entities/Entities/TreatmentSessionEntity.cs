using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("TreatmentSessions")]
	public class TreatmentSessionEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → TreatmentPlans: thuộc gói nào</summary>
		public int? TreatmentPlanId { get; set; }

		/// <summary>Buổi thứ mấy: 1, 2, 3...</summary>
		public int? SessionNumber { get; set; }

		/// <summary>Tên buổi: 'Buổi 1: Tẩy da chết', 'Buổi 2: Laser nhẹ'</summary>
		[MaxLength(250)]
		public string? SessionName { get; set; }

		/// <summary>Mô tả chi tiết quy trình buổi này</summary>
		public string? Description { get; set; }

		/// <summary>Thời lượng buổi (phút) — có thể khác nhau mỗi buổi</summary>
		public int? Duration { get; set; }

		// Navigation properties
		[ForeignKey(nameof(TreatmentPlanId))]
		public virtual TreatmentPlanEntity? TreatmentPlan { get; set; }

		public virtual ICollection<SessionProductEntity> SessionProducts { get; set; } = [];
		public virtual ICollection<CustomerTreatmentSessionEntity> CustomerTreatmentSessions { get; set; } = [];
	}
}
