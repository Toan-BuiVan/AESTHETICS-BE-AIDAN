using System.ComponentModel.DataAnnotations;

namespace Aesthetics.Entities.Models.RequestModel
{
    /// <summary>
    /// Request tạo URL thanh toán VNPay
    /// </summary>
    public class CreateVnPayPaymentUrlRequest
    {
        [Required(ErrorMessage = "InvoiceId là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "InvoiceId phải lớn hơn 0")]
        public int InvoiceId { get; set; }
    }

    /// <summary>
    /// Request tạo URL thanh toán Momo
    /// </summary>
    public class CreateMomoPaymentUrlRequest
    {
        [Required(ErrorMessage = "InvoiceId là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "InvoiceId phải lớn hơn 0")]
        public int InvoiceId { get; set; }
    }

    /// <summary>
    /// Request cập nhật trạng thái thanh toán
    /// </summary>
    public class UpdateInvoicePaymentRequest
    {
        [Required(ErrorMessage = "InvoiceId là bắt buộc")]
        public int InvoiceId { get; set; }

        [Required(ErrorMessage = "PaidAmount là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "PaidAmount phải lớn hơn 0")]
        public decimal PaidAmount { get; set; }

        [Required(ErrorMessage = "PaymentMethod là bắt buộc")]
        public string PaymentMethod { get; set; } // "VNPay", "Momo", "TienMat"
    }

    /// <summary>
    /// Response thông tin hóa đơn
    /// </summary>
    public class InvoicePaymentInfoModel
    {
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingBalance { get; set; }
        public string Status { get; set; } // "ChuaThanhToan", "ThanhToanMotPhan", "DaThanhToan"
        public string PaymentMethod { get; set; }
        public DateTime? DateCreated { get; set; }
        public string OrderStatus { get; set; }
    }
}