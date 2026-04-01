using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
	public class RequestUpdateStaff
	{
		public class UpdateStaffRequest
		{
			public int AccountId { get; set; }

			/// <summary>Họ tên nhân viên</summary>
			public string? FullName { get; set; }

			/// <summary>Ngày sinh</summary>
			public DateTime? DateBirth { get; set; }
			/// <summary>Email</summary>
			public string? Email { get; set; }
			public string? Sex { get; set; }

			/// <summary>Số điện thoại</summary>
			public string? Phone { get; set; }

			/// <summary>Địa chỉ</summary>
			public string? Address { get; set; }

			/// <summary>Số CCCD/CMND</summary>
			public string? IDCard { get; set; }

			/// <summary>URL hình ảnh nhân viên</summary>
			public string? StaffImage { get; set; }

			/// <summary>true = Là bác sĩ, false = Không phải</summary>
			public bool? IsDoctor { get; set; }

			/// <summary>0 = Y tá, 1 = Bác sĩ</summary>
			public int? DoctorLevel { get; set; }

			/// <summary>Bằng cấp: 'ThS', 'TS', 'PGS'...</summary>
			public string? Degree { get; set; }

			/// <summary>Chuyên khoa: 'Da liễu', 'Phẫu thuật thẩm mỹ'...</summary>
			public string? Specialization { get; set; }

			/// <summary>Số giấy phép hành nghề (duy nhất)</summary>
			public string? LicenseNumber { get; set; }

			/// <summary>Số năm kinh nghiệm</summary>
			public int? ExperienceYears { get; set; }

			/// <summary>Tiểu sử / giới thiệu bản thân</summary>
			public string? Biography { get; set; }

			public int? EmploymentStatus { get; set; }
		}
	}
}
