using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Entities.Entities
{
	[Table("InvoiceDetails")]
	public class InvoiceDetailEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>FK → Invoices: thuộc hóa đơn nào</summary>
		public int? InvoiceId { get; set; }

		/// <summary>FK → Products: sản phẩm (null nếu là dịch vụ)</summary>
		public int? ProductId { get; set; }

		/// <summary>FK → Services: dịch vụ (null nếu là sản phẩm)</summary>
		public int? ServiceId { get; set; }

		/// <summary>FK → TreatmentPlans: gói liệu trình (null nếu đơn lẻ)</summary>
		public int? TreatmentPlanId { get; set; }

		/// <summary>FK → Vouchers: voucher riêng cho dòng này (nếu có)</summary>
		public int? VoucherId { get; set; }

		/// <summary>Phần trăm giảm cho dòng này</summary>
		[Column(TypeName = "decimal(5,2)")]
		public decimal DiscountValue { get; set; }

		/// <summary>Đơn giá tại thời điểm mua</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal? Price { get; set; }

		/// <summary>Số lượng</summary>
		public int Quantity { get; set; } = 1;

		/// <summary>= Price * Quantity * (1 - DiscountValue/100)</summary>
		[Column(TypeName = "decimal(18,2)")]
		public decimal? TotalMoney { get; set; }

		/// <summary>Trạng thái dòng: DaXuLy, DangCho</summary>
		[MaxLength(50)]
		public string? Status { get; set; }

		/// <summary>'Nhap' = nhập hàng, 'Ban' = bán hàng</summary>
		[MaxLength(50)]
		public string? Type { get; set; }

		/// <summary>false = Chưa đánh giá, true = Đã đánh giá</summary>
		public bool StatusComment { get; set; }

		// Navigation properties
		[ForeignKey(nameof(InvoiceId))]
		public virtual InvoiceEntity? Invoice { get; set; }

		[ForeignKey(nameof(ProductId))]
		public virtual ProductEntity? Product { get; set; }

		[ForeignKey(nameof(ServiceId))]
		public virtual ServiceEntity? Service { get; set; }

		[ForeignKey(nameof(TreatmentPlanId))]
		public virtual TreatmentPlanEntity? TreatmentPlan { get; set; }

		[ForeignKey(nameof(VoucherId))]
		public virtual VoucherEntity? Voucher { get; set; }
	}
}
