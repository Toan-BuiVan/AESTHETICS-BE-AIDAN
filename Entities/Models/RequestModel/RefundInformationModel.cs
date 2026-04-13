namespace Aesthetics.DTO.NetCore.DataObject.Model.VnPay
{
    /// <summary>
    /// Model thông tin hoàn tiền cho VNPay
    /// </summary>
    public class RefundInformationModel
    {
        /// <summary>ID hóa đơn/đơn hàng</summary>
        public string OrderID { get; set; }

        /// <summary>Số tiền hoàn</summary>
        public decimal RefundAmount { get; set; }

        /// <summary>Mã giao dịch gốc từ VNPay (vnp_TransactionNo)</summary>
        public string TransactionNo { get; set; }

        /// <summary>Lý do hoàn hàng</summary>
        public string RefundReason { get; set; }
    }
}