using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("CustomerTreatmentSessions")]
	public class CustomerTreatmentSessionEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → CustomerTreatmentPlans: thuộc gói đăng ký nào</summary>
		public int? CustomerTreatmentPlanId { get; set; }

		/// <summary>FK → TreatmentSessions: buổi template nào (buổi 1, 2...)</summary>
		public int? TreatmentSessionId { get; set; }
		/// <summary>
		/// ChoDatLich: Chờ đặt lịch khám,
		/// DaDatLich: đã đặt lịch hẹn,
		/// DangThucHien: đang thực hiện,
		/// HoanThanh: buổi này đã xong,
		/// KhachHuy: kahchs hủy đặt lịch (no-show)
		/// </summary>
		/// 

		[MaxLength(50)]
		public string? Status { get; set; }

		// Navigation properties
		[ForeignKey(nameof(CustomerTreatmentPlanId))]
		public virtual CustomerTreatmentPlanEntity? CustomerTreatmentPlan { get; set; }

		[ForeignKey(nameof(TreatmentSessionId))]
		public virtual TreatmentSessionEntity? TreatmentSession { get; set; }
	}
}
