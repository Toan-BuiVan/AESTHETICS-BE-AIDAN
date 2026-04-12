using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
	public class CreateStaffShift
	{
		public int? StaffId { get; set; }
		public DateTime? Date { get; set; }

		[Column(TypeName = "datetime")]
		public DateTime? StartDate { get; set; }

		[Column(TypeName = "datetime")]
		public DateTime? EndDate { get; set; }

	}
	public class DeleteStaffShift
	{
		public int Id { get; set; }
	}
	public class GetStaffShift : BaseSearchModel
	{
		public int? Id { get; set; }
		public int? StaffId { get; set; }
		public DateTime? Date { get; set; }
	}
}
