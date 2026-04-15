using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aesthetics.Entities.Entities
{
	[Table("Refunds")]
	public class RefundEntity : Aesthetics.Entities.BaseEntity.BaseEntity
	{
		/// <summary>Liên kết đến hoá đơn cần hoàn tiền</summary>
		[ForeignKey("Invoice")]
		public int? InvoiceId { get; set; }

		public int? CustomerId { get; set; }

		/// <summary>Số tiền hoàn lại</summary>
		[Column(TypeName = "decimal(18, 2)")]
		public decimal? RefundAmount { get; set; }

		/// <summary>Lý do hoàn tiền</summary>
		[StringLength(1000)]
		public string? RefundReason { get; set; }

		/// <summary>Hình ảnh chứng minh hoàn tiền (lưu đường dẫn hoặc JSON array)</summary>
		public string? RefundImages { get; set; }

		/// <summary>Phương thức hoàn tiền: BankTransfer, Wallet, Cash</summary>
		[StringLength(50)]
		public string? RefundMethod { get; set; }

		/// <summary>Số tài khoản ngân hàng (nếu hoàn về tài khoản)</summary>
		[StringLength(50)]
		public string? BankAccount { get; set; }

		/// <summary>Tên chủ tài khoản</summary>
		[StringLength(200)]
		public string? BankAccountName { get; set; }

		/// <summary>Tên ngân hàng</summary>
		[StringLength(200)]
		public string? BankName { get; set; }

		/// <summary>Trạng thái hoàn tiền: PendingApproval, Approved, Rejected, Completed, Failed</summary>
		[StringLength(50)]
		public string? Status { get; set; }

		public int? StaffId { get; set; }

		/// <summary>Ngày duyệt hoàn tiền</summary>
		public DateTime? ApprovedDate { get; set; }

		/// <summary>Mã giao dịch hoàn tiền từ VNPay</summary>
		[StringLength(100)]
		public string? RefundTransactionId { get; set; }

		/// <summary>Ngày hoàn tiền thực tế</summary>
		public DateTime? CompletedDate { get; set; }

		/// <summary>Ngày tạo yêu cầu hoàn tiền</summary>
		public DateTime? CreatedDate { get; set; }

		// Navigation properties
		public virtual InvoiceEntity? Invoice { get; set; }
		public virtual CustomerEntity? Customer { get; set; }
		public virtual StaffEntity? Staffs { get; set; }
	}
}