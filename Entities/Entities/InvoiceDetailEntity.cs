using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aesthetics.Entities.Entities
{
	[Table("InvoiceDetails")]
	public class InvoiceDetailEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		[ForeignKey("Invoice")]
		public int? InvoiceId { get; set; }

		[ForeignKey("Product")]
		public int? ProductId { get; set; }

		[ForeignKey("Service")]
		public int? ServiceId { get; set; }

		[ForeignKey("TreatmentPlan")]
		public int? TreatmentPlanId { get; set; }

		[ForeignKey("TreatmentSession")]
		public int? TreatmentSessionId { get; set; }

		//[ForeignKey("Voucher")]
		public int? VoucherId { get; set; }

		/// <summary>Giá đơn vị của sản phẩm/dịch vụ/gói</summary>
		[Column(TypeName = "decimal(18, 2)")]
		public decimal? Price { get; set; }

		/// <summary>Số lượng</summary>
		public int? Quantity { get; set; } = 1;

		/// <summary>Tổng giá tiền gốc (Price × Quantity) - chưa áp dụng voucher</summary>
		[Column(TypeName = "decimal(18, 2)")]
		public decimal? TotalMoney { get; set; }

		/// <summary>Số tiền được giảm từ voucher</summary>
		[Column(TypeName = "decimal(18, 2)")]
		public decimal? DiscountValue { get; set; }

		/// <summary>Tổng giá tiền sau khi áp dụng voucher (TotalMoney - DiscountValue)</summary>
		[Column(TypeName = "decimal(18, 2)")]
		public decimal? FinalPrice { get; set; }

		/// <summary>Trạng thái thanh toán: ChuaThanhToan, ThanhToanMotPhan, DaThanhToan</summary>
		public string? Status { get; set; }

		/// <summary>Loại: Ban, etc</summary>
		public string? Type { get; set; }

		/// <summary>Ghi chú trạng thái</summary>
		public bool? StatusComment { get; set; }

		// Navigation properties
		public virtual InvoiceEntity? Invoice { get; set; }
		public virtual ProductEntity? Product { get; set; }
		public virtual ServiceEntity? Service { get; set; }
		public virtual TreatmentSessionEntity? TreatmentSession { get; set; }
		public virtual TreatmentPlanEntity? TreatmentPlan { get; set; }
		//public virtual VoucherEntity? Voucher { get; set; }
	}
}
