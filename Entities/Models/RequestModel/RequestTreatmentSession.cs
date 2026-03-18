using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
	public class CreateTreatmentSession
	{
		public int? TreatmentPlanId { get; set; }
		public int? SessionNumber { get; set; }
		public string? SessionName { get; set; }
		public string? Description { get; set; }
		public int? Duration { get; set; }
	}
	public class UpdateTreatmentSession
	{
		public int? Id { get; set; }
		public int? TreatmentPlanId { get; set; }
		public int? SessionNumber { get; set; }
		public string? SessionName { get; set; }
		public string? Description { get; set; }
		public int? Duration { get; set; }
	}
	public class DeleteTreatmentSession
	{
		public int? Id { get; set; }
	}
	public class TreatmentSessionGet : BaseSearchModel
	{
		public int? TreatmentPlanId { get; set; }
	}
}