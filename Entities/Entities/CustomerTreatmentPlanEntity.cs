using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("CustomerTreatmentPlans")]
	public class CustomerTreatmentPlanEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Customers: khách nào đăng ký</summary>
		public int? CustomerId { get; set; }

		/// <summary>FK → TreatmentPlans: gói nào</summary>
		public int? TreatmentPlanId { get; set; }
		/// <summary>
		/// DangThucHien: đang chạy liệu trình,
		/// HoanThanh: đã xong tất cả buổi,
		/// TamDung: khách xin tạm dừng,
		/// Huy: khách hủy giữa chừng
		/// </summary>
		[MaxLength(50)]
		public string? Status { get; set; }
		// Navigation properties
		[ForeignKey(nameof(CustomerId))]
		public virtual CustomerEntity? Customer { get; set; }

		[ForeignKey(nameof(TreatmentPlanId))]
		public virtual TreatmentPlanEntity? TreatmentPlan { get; set; }

		public virtual ICollection<CustomerTreatmentSessionEntity> CustomerTreatmentSessions { get; set; } = [];
		public virtual ICollection<AppointmentEntity> Appointments { get; set; } = [];
	}
}
