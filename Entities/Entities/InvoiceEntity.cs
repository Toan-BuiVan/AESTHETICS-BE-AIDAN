using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aesthetics.Entities.Entities
{
	[Table("Invoices")]
	public class InvoiceEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		[ForeignKey("Customer")]
		public int? CustomerId { get; set; }

		[ForeignKey("Staff")]
		public int? StaffId { get; set; }

		[ForeignKey("Service")]
		public int? ServiceId { get; set; }

		[ForeignKey("TreatmentPlan")]
		public int? TreatmentPlanId { get; set; }

		[ForeignKey("TreatmentSession")] 
		public int? TreatmentSessionId { get; set; }

		//[ForeignKey("Voucher")]
		public int? VoucherId { get; set; }

		/// <summary>Tổng giá tiền gốc (chưa áp dụng voucher)</summary>
		[Column(TypeName = "decimal(18, 2)")]
		public decimal? TotalMoney { get; set; }

		/// <summary>Số tiền được giảm từ voucher</summary>
		[Column(TypeName = "decimal(18, 2)")]
		public decimal? DiscountValue { get; set; }

		/// <summary>Tổng giá tiền sau khi áp dụng voucher</summary>
		[Column(TypeName = "decimal(18, 2)")]
		public decimal? FinalPrice { get; set; }

		/// <summary>Số tiền đã thanh toán</summary>
		[Column(TypeName = "decimal(18, 2)")]
		public decimal? PaidAmount { get; set; }

		/// <summary>Số tiền còn nợ</summary>
		[Column(TypeName = "decimal(18, 2)")]
		public decimal? OutstandingBalance { get; set; }

		/// <summary>Trạng thái thanh toán: ChuaThanhToan, ThanhToanMotPhan, DaThanhToan</summary>
		public string? Status { get; set; }

		/// <summary>✅ Thời gian ghi nhận giao dịch thanh toán (GMT+7) - dùng cho VNPAY refund</summary>
		public DateTime? PaymentDate { get; set; }

		/// <summary>Mã giao dịch từ VNPAY</summary>
		public string? TransactionId { get; set; }

		/// <summary>Phương thức thanh toán: VNPAY, MOMO, etc</summary>
		public string? PaymentMethod { get; set; }

		/// <summary>Loại hóa đơn</summary>
		public string? Type { get; set; }

		/// <summary>Trạng thái đơn hàng</summary>
		public string? OrderStatus { get; set; }

		/// <summary>Ngày tạo hóa đơn</summary>
		public DateTime? DateCreated { get; set; }

		/// <summary>Cờ kiểm tra hoàn tiền</summary>
		public bool? IsRefund { get; set; }

		/// <summary>Địa chỉ giao hàng</summary>
		public string? ShipToAddress { get; set; }

		/// <summary>✅ Cờ đánh dấu hóa đơn đã giao hàng</summary>
		public bool? IsDelivered { get; set; }

		// ✅ Navigation properties
		public virtual CustomerEntity? Customer { get; set; }
		public virtual StaffEntity? Staff { get; set; }
		public virtual ServiceEntity? Service { get; set; }
		public virtual TreatmentPlanEntity? TreatmentPlan { get; set; }
		public virtual TreatmentSessionEntity? TreatmentSession { get; set; }

		//public virtual VoucherEntity? Voucher { get; set; }
		public virtual ICollection<InvoiceDetailEntity>? InvoiceDetails { get; set; }
		public virtual ICollection<PerformanceLogEntity>? PerformanceLogs { get; set; }
	}
}
