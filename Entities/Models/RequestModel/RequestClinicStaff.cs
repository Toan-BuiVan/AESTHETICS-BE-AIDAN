using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class CreateClinicStaff
    {
		public int? ClinicId { get; set; }

		public int? StaffId { get; set; }
	}

	public class UpdateClinicStaff
	{
		public int? Id { get; set; }
		public int? ClinicId { get; set; }
		public int? StaffId { get; set; }
	}

	public class DeleteClinicStaff
	{
		public int? Id { get; set; }
	}

	public class GetClinicStaff : BaseSearchModel
	{
		public int? Id { get; set; }
		public int? ClinicId { get; set; }
	}
}
