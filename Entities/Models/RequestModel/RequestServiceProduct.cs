using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Models.RequestModel
{
    public class CreateServiceProduct
    {
		public int? ServiceId { get; set; }

		public int? ProductId { get; set; }

		public int? QuantityUsed { get; set; }
	}

	public class UpdateServiceProduct
	{
		public int Id { get; set; }
		public int? ServiceId { get; set; }
		public int? ProductId { get; set; }
		public int? QuantityUsed { get; set; }
	}

	public class DeleteServiceProduct
	{
		public int Id { get; set; }
	}

	public class MyClass
	{
		
	}
}
