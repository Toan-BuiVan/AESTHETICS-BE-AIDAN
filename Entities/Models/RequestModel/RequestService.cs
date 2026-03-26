using Aesthetics.Entities.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class CreateService
    {
		public int? ServiceTypeId { get; set; }

		public string? ServiceName { get; set; }

		public string? Description { get; set; }

		public string? ServiceImage { get; set; }

		public decimal? Price { get; set; } = 0;

		public int? Duration { get; set; } = 0;

		public int? SessionInterval { get; set; }

		public bool? IsCourse { get; set; }
	}

	public class UpdateService
	{
		public int? Id { get; set; }

		public int? ServiceTypeId { get; set; }

		public string? ServiceName { get; set; }

		public string? Description { get; set; }

		public string? ServiceImage { get; set; }

		public decimal? Price { get; set; } = 0;

		public int? Duration { get; set; } = 0;
		public bool? IsCourse { get; set; }
	}

	public class DeleteService
	{
		public int? Id { get; set; }
	}

	public class ServiceGet : BaseSearchModel
	{
		public int? Id { get; set; }
		public string? ServiceName { get; set; }
		public int? ServiceTypeId { get; set; }
		public bool? IsCourse { get; set; }
	}

	public class exportservice 
	{
		public List<int>? ServiceIds { get; set; }
	}
}
