using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.ResponseModel
{
	public class StaffResponseModel
	{
		public int Id { get; set; }

		public int AccountId { get; set; }

		public string? FullName { get; set; }

		public DateTime? DateBirth { get; set; }

		public string? Sex { get; set; }

		public string? Phone { get; set; }

		public string? Address { get; set; }

		public string? IDCard { get; set; }

		public int? SalesPoints { get; set; }

		public string? StaffImage { get; set; }

		public int? EmploymentStatus { get; set; }

		public bool? IsDoctor { get; set; }

		public int? DoctorLevel { get; set; }

		public string? Degree { get; set; }

		public string? Specialization { get; set; }

		public string? LicenseNumber { get; set; }

		public int? ExperienceYears { get; set; }

		public string? Biography { get; set; }

		public string? AccountName { get; set; }
		public string? Email { get; set; }

		public List<int>? ClinicIds { get; set; }
	}
}
