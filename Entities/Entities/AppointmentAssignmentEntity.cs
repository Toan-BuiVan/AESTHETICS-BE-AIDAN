using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("AppointmentAssignments")]
	public class AppointmentAssignmentEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Appointments: thuộc lịch hẹn nào</summary>
		public int? AppointmentId { get; set; }

		/// <summary>FK → Clinics: thực hiện ở phòng nào</summary>
		public int? ClinicId { get; set; }

		/// <summary>FK → ServiceTypes: loại dịch vụ</summary>
		public int? ServiceTypeId { get; set; }

		/// <summary>FK → Staffs: bác sĩ/nhân viên thực hiện</summary>
		public int? StaffId { get; set; }

		/// <summary>FK → Services: dịch vụ cụ thể</summary>
		public int? ServiceId { get; set; }

		/// <summary>Thứ tự thực hiện: 1, 2, 3... (nếu combo nhiều dịch vụ)</summary>
		public int? NumberOrder { get; set; }

		/// <summary>Ngày/giờ thực hiện</summary>
		public DateTime? AssignedDate { get; set; }

		/// <summary>false = Chưa thực hiện, true = Đã hoàn thành</summary>
		public int Status { get; set; }

		/// <summary>Số lượng dịch vụ</summary>
		public int QuantityServices { get; set; } = 1;

		/// <summary>Giá cho phân công này</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal? Price { get; set; }

		/// <summary>0 = Chưa thanh toán, 1 = Đã thanh toán toàn bộ, 2 = thanh toán 1 phần </summary>
		public int? PaymentStatus { get; set; }

		/// <summary>FK → Equipments: thiết bị sử dụng (tránh trùng)</summary>
		public int? EquipmentId { get; set; }

		// Navigation properties
		[ForeignKey(nameof(AppointmentId))]
		public virtual AppointmentEntity? Appointment { get; set; }

		[ForeignKey(nameof(ClinicId))]
		public virtual ClinicEntity? Clinic { get; set; }

		[ForeignKey(nameof(ServiceTypeId))]
		public virtual ServiceTypeEntity? ServiceType { get; set; }

		[ForeignKey(nameof(StaffId))]
		public virtual StaffEntity? Staff { get; set; }

		[ForeignKey(nameof(ServiceId))]
		public virtual ServiceEntity? Service { get; set; }

		[ForeignKey(nameof(EquipmentId))]
		public virtual EquipmentEntity? Equipment { get; set; }
	}
}
