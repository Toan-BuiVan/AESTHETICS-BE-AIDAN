using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("CartProducts")]
	public class CartProductEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Carts: thuộc giỏ hàng nào</summary>
		public int? CartId { get; set; }

		/// <summary>FK → Products: sản phẩm (null nếu là dịch vụ)</summary>
		public int? ProductId { get; set; }

		/// <summary>Số lượng</summary>
		public int Quantity { get; set; } = 1;

		/// <summary>Giá tại thời điểm thêm vào giỏ (snapshot)</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal? PriceAtAdd { get; set; }

		/// <summary>Ngày thêm vào giỏ</summary>
		public DateTime? CreateDate { get; set; }

		// Navigation properties
		[ForeignKey(nameof(CartId))]
		public virtual CartEntity? Cart { get; set; }

		[ForeignKey(nameof(ProductId))]
		public virtual ProductEntity? Product { get; set; }
	}
}
