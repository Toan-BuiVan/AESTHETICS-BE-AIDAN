using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class CreateProduct
    {
		public int ServiceTypeId { get; set; }

		public int SupplierId { get; set; }

		public string ProductName { get; set; }

		public string Description { get; set; }

		public decimal SellingPrice { get; set; } = 0;

		public int Quantity { get; set; } = 0;

		public string Unit { get; set; }

		public int MinimumStock { get; set; } = 0;

		public string ProductImages { get; set; }

		public decimal CostPrice { get; set; }
	}

	public class updateProduct
	{
		public int Id { get; set; }
		public int? ServiceTypeId { get; set; }

		public int? SupplierId { get; set; }

		public string? ProductName { get; set; }

		public string? Description { get; set; }

		public decimal? SellingPrice { get; set; } = 0;

		public int? Quantity { get; set; } = 0;

		public string? Unit { get; set; }

		public int? MinimumStock { get; set; } = 0;

		public string? ProductImages { get; set; }

		public decimal? CostPrice { get; set; } 
		public string? Status { get; set; }
	}

	public class deleteProduct
	{
		public int Id { get; set; }
	}

	public class getproduct: BaseSearchModel
	{
		public int? Id { get; set; }
		public string? ProductName { get; set; }
		public string? SupplierName { get; set; }
		public string? ServiceTypeName { get; set; }
		public int? ProductId { get; set; }
		public int? ServiceTypeId { get; set; }
		public int? SupplierId { get; set; }
	}

	public class exportproduct
	{
		public List<int>? ProductIds { get; set; }
	}
}
