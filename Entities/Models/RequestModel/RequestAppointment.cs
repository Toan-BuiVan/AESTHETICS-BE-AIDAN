using Aesthetics.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class CreateAppointment
    {
		public int? CustomerId { get; set; }
		public int? StaffId { get; set; }
		public int? ServiceId { get; set; }
		public DateTime? StartTime { get; set; }
		public EnumTreatmentPlans? TypeInvoice { get; set; }
		public decimal? PaidAmount { get; set; }
		public int? CustomerTreatmentPlanId { get; set; }
		public int? VoucherId { get; set; }
		public string? PaymentMethod { get; set; }
	}

	public class DeleteAppointment
	{
		public int? Id { get; set; }
	}

	public class AppointmentGet : BaseSearchModel
	{
		public int? CustomerId { get; set; }
		public int? StaffId { get; set; }
	}
}
