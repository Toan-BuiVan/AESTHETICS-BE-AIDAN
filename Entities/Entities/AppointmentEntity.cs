using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Appointments")]
	public class AppointmentEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Customers: khách đặt lịch</summary>
		public int? CustomerId { get; set; }

		/// <summary>FK → Staffs: bác sĩ/nhân viên thực hiện</summary>
		public int? StaffId { get; set; }

		/// <summary>FK → Services: dịch vụ đã đặt</summary>
		public int? ServiceId { get; set; }

		/// <summary>FK → CustomerTreatmentPlans: thuộc liệu trình nào (null = đơn lẻ)</summary>
		public int? CustomerTreatmentPlanId { get; set; }

		/// <summary>FK → CustomerTreatmentSessions: liên kết với buổi chữa trị cụ thể</summary>
		public int? CustomerTreatmentSessionId { get; set; }

		/// <summary>Thời gian bắt đầu</summary>
		public DateTime? StartTime { get; set; }

		/// <summary>Ngày tạo lịch hẹn</summary>
		public DateTime? CreationDate { get; set; }

		/// <summary>Trạng thái: DaDat, DangThucHien, HoanThanh, Huy, DoiLich</summary>
		public int? Status { get; set; }

		/// <summary>0 = Chưa thanh toán, 1 = Đã thanh toán toàn bộ, 2 = thanh toán 1 phần </summary>
		public int? PaymentStatus { get; set; }

		/// <summary>Đã gửi email xác nhận đặt lịch hay chưa</summary>
		public bool IsConfirmationEmailSent { get; set; } = false;

		/// <summary>Thời gian gửi email xác nhận</summary>
		public DateTime? ConfirmationEmailSentDate { get; set; }

		/// <summary>Đã gửi email nhắc nhở hay chưa</summary>
		public bool IsReminderEmailSent { get; set; } = false;

		/// <summary>Thời gian gửi email nhắc nhở</summary>
		public DateTime? ReminderEmailSentDate { get; set; }

		/// <summary>Thời gian trước khi gửi nhắc nhở (giờ) - mặc định 24h</summary>
		public int ReminderHoursBefore { get; set; } = 24;

		/// <summary>Cờ kiểm tra comment</summary>
		public bool? IsComment { get; set; } = false;

		// Navigation properties
		[ForeignKey(nameof(CustomerId))]
		public virtual CustomerEntity? Customer { get; set; }

		[ForeignKey(nameof(StaffId))]
		public virtual StaffEntity? Staff { get; set; }

		[ForeignKey(nameof(ServiceId))]
		public virtual ServiceEntity? Service { get; set; }

		[ForeignKey(nameof(CustomerTreatmentPlanId))]
		public virtual CustomerTreatmentPlanEntity? CustomerTreatmentPlan { get; set; }

		[ForeignKey(nameof(CustomerTreatmentSessionId))]
		public virtual CustomerTreatmentSessionEntity? CustomerTreatmentSession { get; set; }

		public virtual ICollection<AppointmentAssignmentEntity> AppointmentAssignments { get; set; } = [];
	}
}