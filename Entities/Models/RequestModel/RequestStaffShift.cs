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

		public bool? ShiftType { get; set; }
	}

	public class UpdateStaffShift
	{
		public int Id { get; set; }
		public int? StaffId { get; set; }
		public DateTime? Date { get; set; }
		public bool? ShiftType { get; set; }
		public bool? Status { get; set; }
	}
	public class DeleteStaffShift
	{
		public int Id { get; set; }
	}
	public class GetStaffShift : BaseSearchModel
	{
		public int? Id { get; set; }
		public int? StaffId { get; set; }
		public bool? ShiftType { get; set; }
		public DateTime? Date { get; set; }
	}
}
