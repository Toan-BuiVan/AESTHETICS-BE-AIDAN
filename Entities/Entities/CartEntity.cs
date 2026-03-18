using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("Carts")]
	public class CartEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Customers: giỏ hàng của khách nào</summary>
		public int? CustomerId { get; set; }

		/// <summary>Ngày tạo giỏ hàng</summary>
		public DateTime? CreationDate { get; set; }

		// Navigation properties
		[ForeignKey(nameof(CustomerId))]
		public virtual CustomerEntity? Customer { get; set; }

		public virtual ICollection<CartProductEntity> CartProductEntitys { get; set; } = [];
	}
}
