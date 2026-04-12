using System;

namespace Aesthetics.Entities.Models.ResponseModel
{
    /// <summary>
    /// Response model cho hoàn tiền hóa đơn
    /// </summary>
    public class RefundResponseModel
    {
        /// <summary>Trạng thái hoàn tiền thành công/thất bại</summary>
        public bool Success { get; set; }

        /// <summary>Thông báo chi tiết</summary>
        public string Message { get; set; }

        /// <summary>ID hóa đơn</summary>
        public int InvoiceId { get; set; }

        /// <summary>Số tiền hoàn</summary>
        public decimal RefundAmount { get; set; }

        /// <summary>Lý do hoàn hàng</summary>
        public string RefundReason { get; set; }

        /// <summary>Mã giao dịch hoàn tiền</summary>
        public string RefundTransactionId { get; set; }

        /// <summary>Phương thức thanh toán</summary>
        public string PaymentMethod { get; set; }

        /// <summary>Ngày hoàn tiền</summary>
        public DateTime? RefundDate { get; set; }

        /// <summary>Số tiền đã thanh toán sau hoàn</summary>
        public decimal NewPaidAmount { get; set; }

        /// <summary>Số tiền còn nợ sau hoàn</summary>
        public decimal NewOutstandingBalance { get; set; }

        /// <summary>Số tiền đã thanh toán trước hoàn</summary>
        public decimal PaidAmount { get; set; }
    }
}