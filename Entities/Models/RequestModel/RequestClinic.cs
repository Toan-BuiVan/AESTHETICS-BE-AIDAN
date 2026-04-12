using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class RequestClinic
    {
        public string ClinicName { get; set; }
		public int? ServiceTypeId { get; set; }
	}

	public class UpdateClinic
	{
		public int Id { get; set; }
		public string? ClinicName { get; set; }
		public bool? ClinicStatus { get; set; }
	}

	public class DeleteClinic
	{
		public int Id { get; set; }
	}

	public class ClinicGet : BaseSearchModel
	{
		public int? Id { get; set; }
		public string? ClinicName { get; set; }
	}
}
