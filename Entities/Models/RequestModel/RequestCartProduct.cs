using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class CreateCartProduct
    {
		public int? CartId { get; set; }

		public int? ProductId { get; set; }

		public int? ServiceId { get; set; }

		public decimal? PriceAtAdd { get; set; }
	}

	public class UpdateCartProduct
	{
		public int? Id { get; set; }
		public int? Quantity { get; set; }
	}

	public class DeleteCartProduct
	{
		public int? Id { get; set; }
	}

	public class GetCartProduct : BaseSearchModel
	{
		public int? Id { get; set; }
	}
}
