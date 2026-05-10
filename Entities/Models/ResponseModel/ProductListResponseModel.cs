using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.ResponseModel
{
	public class ProductListResponseModel
	{
		public int Id { get; set; }
		public string? ServiceTypeName { get; set; }
		public string? SupplierName { get; set; }
		public string? ProductName { get; set; }
		public string? Description { get; set; }
		public decimal? SellingPrice { get; set; }
		public decimal? CostPrice { get; set; }
		public int Quantity { get; set; }
		public int? MinimumStock { get; set; }
		public string? Unit { get; set; }
		public string? ProductImages { get; set; }
	}
}
