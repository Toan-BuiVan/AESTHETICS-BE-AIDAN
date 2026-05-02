using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aesthetics.Entities.Enum;

namespace Aesthetics.Entities.Models.RequestModel
{
	/// <summary>
	/// Mục hàng trong hóa đơn với quantity
	/// </summary>
	public class InvoiceLineItem
	{
		/// <summary>ID sản phẩm</summary>
		public int ProductId { get; set; }

		/// <summary>Số lượng (mặc định = 1)</summary>
		public int Quantity { get; set; } = 1;
	}

	/// <summary>
	/// Request model để tạo hóa đơn với nhiều sản phẩm
	/// </summary>
	public class CreateInvoice
	{
		public int? CustomerId { get; set; }
		public int? StaffId { get; set; }

		/// <summary>Danh sách sản phẩm với số lượng</summary>
		public List<InvoiceLineItem>? LineItems { get; set; }
		public string? Type { get; set; }

		public int? VoucherId { get; set; }

		public decimal PaidAmount { get; set; } = 0;

		public string? PaymentMethod { get; set; } = "TienMat";

		public EnumTreatmentPlans? TypeInvoice { get; set; }

		public string? Notes { get; set; }
	}

	public class GetInvoice : BaseSearchModel
	{
		public int? CustomerId { get; set; }

		public int? StaffId { get; set; }

		/// <summary>🆕 Lọc theo danh sách OrderStatus: DangChoXuLy, DangGiao, DaGiao, KhachHuy</summary>
		public List<string>? OrderStatuses { get; set; }

		public string? Type { get; set; }

		/// <summary>Lọc theo trạng thái: ChuaThanhToan, DaThanhToan, ThanhToanMotPhan</summary>
		public string? Status { get; set; }

		/// <summary>Lọc theo khoảng ngày bắt đầu</summary>
		public DateTime? StartDate { get; set; }

		/// <summary>Lọc theo khoảng ngày kết thúc</summary>
		public DateTime? EndDate { get; set; }
	}

	/// <summary>
	/// Request model để cập nhật trạng thái thanh toán của hóa đơn
	/// Tự động tính toán lại status dựa trên tỷ lệ thanh toán
	/// </summary>
	public class UpdateInvoicePaymentStatus
	{
		public int InvoiceId { get; set; }

		/// <summary>
		/// Số tiền thanh toán thêm
		/// - Có thể là thanh toán toàn bộ hoặc thanh toán một phần
		/// - Sẽ được cộng vào PaidAmount hiện tại
		/// </summary>
		public decimal AdditionalPaymentAmount { get; set; }

		/// <summary>
		/// Phương thức thanh toán
		/// Các giá trị hợp lệ: TienMat, ChuyenKhoan, TheNganHang, MoMo, VNPay
		/// </summary>
		public string? PaymentMethod { get; set; }

		/// <summary>
		/// Ghi chú thanh toán (tùy chọn)
		/// Dùng để ghi lại lý do, chi tiết hay bất kỳ thông tin liên quan đến giao dịch thanh toán
		/// </summary>
		public string? PaymentNote { get; set; }
	}

	public class updateinvoiceorderstatus 
	{
		public int invoiceId { get; set; }
	    public string orderStatus { get; set; }
	}

	/// <summary>
	/// Request model để update status Invoice
	/// </summary>
	public class UpdateInvoiceStatusRequest
	{
		/// <summary>ID hóa đơn</summary>
		public int InvoiceId { get; set; }

		/// <summary>Status mới: ChuaThanhToan, ThanhToanMotPhan, DaThanhToan, KhachHuy</summary>
		public string NewStatus { get; set; }
	}

	public class ExportInvoiceOrder 
	{
		public List<int>? invoiceIds { get; set; }
	}

}