using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class RequestSupplier
    {
		public string SupplierName { get; set; }
		public string Address { get; set; }
		public string Phone { get; set; }
	}

	public class SupplierUpdate
	{
		public int Id { get; set; }
		public string? SupplierName { get; set; }
		public string? Address { get; set; }
		public string? Phone { get; set; }
	}

	public class SupplierDelete
	{
		public int Id { get; set; }
	}

	public class SupplierGet : BaseSearchModel
	{
		public int? Id { get; set; }
		public string? SupplierName { get; set; }
	}
}
