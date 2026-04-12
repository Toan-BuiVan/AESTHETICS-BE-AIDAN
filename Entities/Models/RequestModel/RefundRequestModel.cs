namespace Aesthetics.Entities.Models.RequestModel
{
    /// <summary>
    /// Request model cho hoàn tiền hóa đơn
    /// </summary>
    public class RefundRequestModel
    {
        /// <summary>ID hóa đơn cần hoàn tiền</summary>
        public int InvoiceId { get; set; }

        /// <summary>Số tiền hoàn</summary>
        public decimal RefundAmount { get; set; }

        /// <summary>Lý do hoàn hàng</summary>
        public string RefundReason { get; set; }
    }
}