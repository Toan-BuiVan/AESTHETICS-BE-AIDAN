using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("CartProductEntitys")]
	public class CartProductEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Carts: thuộc giỏ hàng nào</summary>
		public int? CartId { get; set; }

		/// <summary>FK → Products: sản phẩm (null nếu là dịch vụ)</summary>
		public int? ProductId { get; set; }

		/// <summary>FK → Services: dịch vụ (null nếu là sản phẩm)</summary>
		public int? ServiceId { get; set; }

		/// <summary>FK → TreatmentPlans: gói liệu trình (null nếu mua lẻ)</summary>
		public int? TreatmentPlanId { get; set; }

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

		[ForeignKey(nameof(ServiceId))]
		public virtual ServiceEntity? Service { get; set; }

		[ForeignKey(nameof(TreatmentPlanId))]
		public virtual TreatmentPlanEntity? TreatmentPlan { get; set; }
	}
}
