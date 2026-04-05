using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.ResponseModel
{
    public class ServiceResponseModel
    {
		public int? Id { get; set; }
		public int? ServiceTypeId { get; set; }
		public string? ServiceTypeName { get; set; }
		public string? ServiceName { get; set; }
		public string? Description { get; set; }
		public string? ServiceImage { get; set; }
		public decimal? Price { get; set; }
		public int? Duration { get; set; }
		public bool? IsCourse { get; set; }
	}
}
