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

		/// <summary>FK → Appointments: liên kết lịch hẹn (1:1, null nếu chưa đặt)</summary>
		public int? AppointmentId { get; set; }

		/// <summary>Ngày dự kiến thực hiện (tính từ SessionInterval)</summary>
		public DateTime? ScheduledDate { get; set; }

		/// <summary>Ngày thực hiện thực tế</summary>
		public DateTime? ActualDate { get; set; }

		/// <summary>
		/// ChuaThucHien: chưa đến lượt,
		/// ChoDatLich: Chờ đặt lịch khám,
		/// DaDatLich: đã đặt lịch hẹn,
		/// DangThucHien: đang thực hiện,
		/// HoanThanh: buổi này đã xong,
		/// BoLo: khách không đến (no-show)
		/// </summary>
		/// 

		[MaxLength(50)]
		public string? Status { get; set; } 

		/// <summary>FK → Staffs: bác sĩ thực hiện buổi này</summary>
		public int? StaffId { get; set; }

		/// <summary>Ghi chú bác sĩ sau buổi: phản ứng da, kết quả...</summary>
		public string? Notes { get; set; }

		/// <summary>URL ảnh TRƯỚC khi thực hiện (so sánh kết quả)</summary>
		public string? BeforeImage { get; set; }

		/// <summary>URL ảnh SAU khi thực hiện</summary>
		public string? AfterImage { get; set; }

		// Navigation properties
		[ForeignKey(nameof(CustomerTreatmentPlanId))]
		public virtual CustomerTreatmentPlanEntity? CustomerTreatmentPlan { get; set; }

		[ForeignKey(nameof(TreatmentSessionId))]
		public virtual TreatmentSessionEntity? TreatmentSession { get; set; }

		[ForeignKey(nameof(AppointmentId))]
		public virtual AppointmentEntity? Appointment { get; set; }

		[ForeignKey(nameof(StaffId))]
		public virtual StaffEntity? Staff { get; set; }
	}
}
