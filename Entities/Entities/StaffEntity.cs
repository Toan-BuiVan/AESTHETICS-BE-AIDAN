using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Staffs")]
	public class StaffEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Accounts: tài khoản đăng nhập</summary>
		public int AccountId { get; set; }

		/// <summary>Họ tên nhân viên</summary>
		[MaxLength(250)]
		public string? FullName { get; set; }

		/// <summary>Vai trò: 0 = Nhân viên, 1 = Admin, 2 = Bác sĩ</summary>
		public int Role { get; set; }

		/// <summary>Ngày sinh</summary>
		public DateTime? DateBirth { get; set; }

		/// <summary>Giới tính: 0 = Nam, 1 = Nữ, 2 = Khác</summary>
		public int? Sex { get; set; }

		/// <summary>Số điện thoại</summary>
		[MaxLength(250)]
		public string? Phone { get; set; }

		/// <summary>Địa chỉ</summary>
		[MaxLength(250)]
		public string? Address { get; set; }

		/// <summary>Số CCCD/CMND</summary>
		[MaxLength(250)]
		public string? IDCard { get; set; }

		/// <summary>Điểm bán hàng (chỉ dùng cho nhân viên bán)</summary>
		public int? SalesPoints { get; set; }

		/// <summary>URL hình ảnh nhân viên</summary>
		public string? StaffImage { get; set; }

		/// <summary>0: Active, 1: Probation, 2: Resigned, 3: Leave</summary>
		public int? EmploymentStatus { get; set; }

		/// <summary>true = Là bác sĩ, false = Không phải</summary>
		public bool? IsDoctor { get; set; }

		// ── Các cột dưới đây chỉ có giá trị khi IsDoctor = true ──

		/// <summary>0 = Y tá, 1 = Bác sĩ</summary>
		public int? DoctorLevel { get; set; }

		/// <summary>Bằng cấp: 'ThS', 'TS', 'PGS'...</summary>
		[MaxLength(250)]
		public string? Degree { get; set; }

		/// <summary>Chuyên khoa: 'Da liễu', 'Phẫu thuật thẩm mỹ'...</summary>
		[MaxLength(250)]
		public string? Specialization { get; set; }

		/// <summary>Số giấy phép hành nghề (duy nhất)</summary>
		[MaxLength(100)]
		public string? LicenseNumber { get; set; }

		/// <summary>Số năm kinh nghiệm</summary>
		public int? ExperienceYears { get; set; }

		/// <summary>Tiểu sử / giới thiệu bản thân</summary>
		public string? Biography { get; set; }

		// Navigation properties
		[ForeignKey(nameof(AccountId))]
		public virtual AccountEntity? Account { get; set; }

		public virtual ICollection<AppointmentEntity> Appointments { get; set; } = [];
		public virtual ICollection<ClinicStaffEntity> ClinicStaffs { get; set; } = [];
		public virtual ICollection<AppointmentAssignmentEntity> AppointmentAssignments { get; set; } = [];
		public virtual ICollection<StaffShiftEntity> StaffShifts { get; set; } = [];
		public virtual ICollection<PerformanceLogEntity> PerformanceLogs { get; set; } = [];
		public virtual ICollection<CustomerTreatmentSessionEntity> CustomerTreatmentSessions { get; set; } = [];
	}
}
