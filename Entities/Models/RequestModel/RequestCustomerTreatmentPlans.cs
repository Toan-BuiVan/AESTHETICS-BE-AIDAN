using Aesthetics.Entities.Enum;

namespace Aesthetics.Entities.Models.RequestModel
{
	public class CreateCustomerTreatment
	{
		public int? CustomerId { get; set; }
		public int? StaffId { get; set; }
		public int? TreatmentPlanId { get; set; }
		public List<int>? TreatmentSessionIds { get; set; }
		public bool? IsFullPackage { get; set; }
		public string? Notes { get; set; }
		public int? VoucherId { get; set; }
	}

	public class UpdateCustomerTreatment
	{
		public int? Id { get; set; }
		public string? Status { get; set; }
	}

	public class DeleteCustomerTreatment
	{
		public int? Id { get; set; }
	}

	public class GetCustomerTreatment: BaseSearchModel
	{
		public int? CustomerId { get; set; }
		public string? Status { get; set; }
	}
}
