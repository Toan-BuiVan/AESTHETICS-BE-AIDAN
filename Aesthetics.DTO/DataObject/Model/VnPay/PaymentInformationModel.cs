namespace Aesthetics.DTO.NetCore.DataObject.Model.VnPay
{
    public class PaymentInformationModel
    {
        public string OrderID { get; set; }
        public string Name { get; set; }
        public string OrderDescription { get; set; }
        public double Amount { get; set; }
    }

    public class PaymentResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string VnPayResponseCode { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
    }
}