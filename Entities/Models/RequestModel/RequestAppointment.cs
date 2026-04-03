using Aesthetics.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Aesthetics.Entities.Models.RequestModel
{
	public class CreateAppointment
	{
		public int? CustomerId { get; set; }

		public int? StaffId { get; set; }

		public int? CustomerTreatmentSessionId { get; set; }

		public int? CustomerTreatmentPlanId { get; set; }

		public int? SessionNumber { get; set; }

		public DateTime? StartTime { get; set; }

		public decimal PaidAmount { get; set; } = 0;
		public EnumTreatmentPlans? TypeInvoice { get; set; }
		public EnumTreatmentPlans? PaymentStatus { get; set; }

		public int? VoucherId { get; set; }
		public string? PaymentMethod { get; set; } = "TienMat";
	}

	/// <summary>Mô hình request xóa appointment</summary>
	public class DeleteAppointment
	{
		public int? Id { get; set; }
	}

	/// <summary>Mô hình request lấy danh sách appointment</summary>
	public class AppointmentGet : BaseSearchModel
	{
		public int? CustomerId { get; set; }

		public int? StaffId { get; set; }

		public string? Status { get; set; }

		public DateTime? StartDate { get; set; }

		public DateTime? EndDate { get; set; }
	}

	/// <summary>✅ Mô hình request lấy thời gian trống của bác sĩ</summary>
	public class GetDoctorAvailabilityRequest
	{
		public int DoctorId { get; set; }

		public int? CustomerTreatmentSessionId { get; set; }

		public int? CustomerTreatmentPlanId { get; set; }
		public int? SessionNumber { get; set; }
		public DateTime Date { get; set; }

	}

	public class updateappoint
	{
		public int? CustomerTreatmentSessionId { get; set; }
		/*
		 2 => "In Progress",
		 3 => "Completed",
		 4 => "Cancelled",
		*/
		public int? Status { get; set; }
	}
}
