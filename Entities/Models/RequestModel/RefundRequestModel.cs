using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aesthetics.Entities.Models.RequestModel
{
    /// <summary>
    /// Request model cho hoàn tiền hóa đơn
    /// </summary>
    public class CreateRefundModel
    {
		public int? InvoiceId { get; set; }
		public int? CustomerId { get; set; }
		/// <summary>Lý do hoàn tiền</summary>
		[StringLength(1000)]
		public string? RefundReason { get; set; }

		/// <summary>Hình ảnh chứng minh hoàn tiền (lưu đường dẫn hoặc JSON array)</summary>
		public string? RefundImages { get; set; }

		/// <summary>Phương thức hoàn tiền: BankTransfer, Wallet, Cash</summary>
		[StringLength(50)]
		public string? RefundMethod { get; set; }

		/// <summary>Trạng thái hoàn tiền: PendingApproval, Approved, Rejected, Completed, Failed</summary>
	}

	public class UpdtaeRefundModel
	{
		public int? Id { get; set; }
		public int? StaffId { get; set; }
		public string? Status { get; set; }
	}

	public class getlist : BaseSearchModel
	{
		public int? InvoiceId { get; set; }
		public int? CustomerId { get; set; }
		public int? StaffId { get; set; }
		public DateTime? startdate { get; set; }
		public DateTime? enddate { get; set; }
	}
}