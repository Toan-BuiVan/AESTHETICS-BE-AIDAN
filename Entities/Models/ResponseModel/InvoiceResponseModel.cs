using System;

namespace Aesthetics.Entities.Models.ResponseModel
{
	public class InvoiceResponseModel
	{
		public int Id { get; set; }

		public int? CustomerId { get; set; }
		public string? CustomerName { get; set; }
		public string? CustomerPhone { get; set; }

		public int? StaffId { get; set; }
		public string? StaffName { get; set; }

		public int? VoucherId { get; set; }
		public string? VoucherCode { get; set; }

		public decimal TotalMoney { get; set; }

		public decimal DiscountValue { get; set; }

		public decimal FinalPrice { get; set; }

		public decimal PaidAmount { get; set; }

		public decimal OutstandingBalance { get; set; }

		public string? Status { get; set; }

		public string? OrderStatus { get; set; }

		public string? PaymentMethod { get; set; }

		public DateTime? DateCreated { get; set; }

		public string? Type { get; set; }
	}

	public class InvoiceDetailResponseModel
	{
		public int Id { get; set; }

		public int? InvoiceId { get; set; }

		public int? ProductId { get; set; }
		public string? ProductName { get; set; }
		public decimal? ProductPrice { get; set; }

		public int? ServiceId { get; set; }
		public string? ServiceName { get; set; }
		public decimal? ServicePrice { get; set; }

		public int? TreatmentPlanId { get; set; }
		public string? TreatmentPlanName { get; set; }

		public int? VoucherId { get; set; }
		public string? VoucherCode { get; set; }

		/// <summary>Giá đơn vị</summary>
		public decimal? Price { get; set; }

		/// <summary>Số lượng</summary>
		public int Quantity { get; set; }

		/// <summary>Số tiền được giảm</summary>
		public decimal DiscountValue { get; set; }

		/// <summary>Tổng giá tiền gốc (Price × Quantity) - chưa áp dụng voucher</summary>
		public decimal? TotalMoney { get; set; }

		/// <summary>✅ Tổng giá tiền sau khi áp dụng voucher</summary>
		public decimal? FinalPrice { get; set; }

		/// <summary>Trạng thái thanh toán</summary>
		public string? Status { get; set; }

		/// <summary>Loại: Ban, etc</summary>
		public string? Type { get; set; }

		/// <summary>Ghi chú trạng thái</summary>
		public bool StatusComment { get; set; }
	}

	public class InvoiceDetailFullResponseModel
	{
		public InvoiceResponseModel? Invoice { get; set; }
		public List<InvoiceDetailResponseModel>? InvoiceDetails { get; set; }
	}
}