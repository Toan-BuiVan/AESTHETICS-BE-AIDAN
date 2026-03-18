using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
	public class CreateSessionProduct
	{
		public int? TreatmentSessionId { get; set; }

		public int? ProductId { get; set; }

		public int? QuantityUsed { get; set; }

		public int? ServiceId { get; set; }
	}

	public class UpdateSessionProduct
	{
		public int Id { get; set; }
		public int? QuantityUsed { get; set; }
	}

	public class DeleteSessionProduct
	{
		public int Id { get; set; }
	}

	public class SessionProductGet : BaseSearchModel
	{
		public int? ServiceId { get; set; }
		public int? TreatmentSessionId { get; set; }
	}
}