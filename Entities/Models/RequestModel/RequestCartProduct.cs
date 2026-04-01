using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class CreateCartProduct
    {
		public int? CustomerId { get; set; }

		public int? ProductId { get; set; }

		public int? Quantity { get; set; }

		public decimal? PriceAtAdd { get; set; }
	}

	public class UpdateCartProduct
	{
		public int? CartProductId { get; set; }
		public int? Quantity { get; set; }
	}

	public class DeleteCartProduct
	{
		public int? CartProductId { get; set; }
	}

	public class GetCartProduct : BaseSearchModel
	{
		public int? CustomerId { get; set; }
	}
}
