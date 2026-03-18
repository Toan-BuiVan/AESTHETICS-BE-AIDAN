using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.ResponseModel
{
	public class AccountProfileResponseModel
	{
		public int AccountId { get; set; }
		public string UserName { get; set; } = string.Empty;
		public DateTime CreationDate { get; set; }
		public bool IsDeleted { get; set; }

		public bool IsCustomer { get; set; }   // true = Khách hàng, false = Nhân viên

		// Thông tin chung
		public string? FullName { get; set; }
		public string? Phone { get; set; }
		public string? Address { get; set; }
		public string? Email { get; set; }
		public string? IDCard { get; set; }

		// Thông tin riêng của KHÁCH HÀNG
		public DateTime? DateBirth { get; set; }
		public string? Sex { get; set; }               // "Nam", "Nu", "Khac"
		public string? ReferralCode { get; set; }
		public int AccumulatedPoints { get; set; }
		public int RatingPoints { get; set; }
		public string? RankMember { get; set; }

		// Thông tin riêng của NHÂN VIÊN / BÁC SĨ
		public int? Role { get; set; }                 // 0=Nhân viên, 1=Admin, 2=Bác sĩ
		public bool IsDoctor { get; set; }
		public string? StaffImage { get; set; }
		public int? EmploymentStatus { get; set; }     // 0=Active, 1=Probation...
		public int? DoctorLevel { get; set; }
		public string? Degree { get; set; }
		public string? Specialization { get; set; }
		public string? LicenseNumber { get; set; }
		public int? ExperienceYears { get; set; }
		public string? Biography { get; set; }
		public int? SalesPoints { get; set; }
	}
}
