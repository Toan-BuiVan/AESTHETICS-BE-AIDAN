using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class RequestServiceType
    {
        public string ServiceTypeName { get; set; }
        public string ServiceCategory { get; set; }
        public string Description { get; set; }
	}

	public class UpdateServiceType
	{
		public int Id { get; set; }
		public string? ServiceTypeName { get; set; }
		public string? ServiceCategory { get; set; }
		public string? Description { get; set; }
	}

	public class DeleteServiceType
	{
		public int Id { get; set; }
	}

	public class ServiceTypeGet : BaseSearchModel
	{
		public int? Id { get; set; }
		public string? ServiceTypeName { get; set; }
	}
}
