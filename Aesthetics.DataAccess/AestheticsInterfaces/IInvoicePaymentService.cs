using Aesthetics.DTO.NetCore.DataObject.Model.Momo;
using Aesthetics.DTO.NetCore.DataObject.Model.VnPay;
using Aesthetics.Entities.Models.RequestModel;
using Microsoft.AspNetCore.Http;

namespace Aesthetics.Data.AestheticsInterfaces
{
    public interface IInvoicePaymentService
    {
        /// <summary>
        /// Tạo Payment Model cho VNPay từ thông tin hóa đơn
        /// </summary>
        Task<PaymentInformationModel> GenerateVnPayPaymentUrl(int invoiceId, HttpContext context);

        /// <summary>
        /// Xử lý callback khi VNPay thanh toán thành công
        /// </summary>
        Task<bool> ProcessVnPayCallback(IQueryCollection collections);

        /// <summary>
        /// Tạo URL thanh toán Momo cho hóa đơn
        /// </summary>
        Task<OrderInfoModel> GenerateMomoPaymentUrl(int invoiceId);

        /// <summary>
        /// Xử lý callback khi Momo thanh toán thành công
        /// </summary>
        Task<bool> ProcessMomoCallback(IQueryCollection collections);

        /// <summary>
        /// Cập nhật trạng thái thanh toán hóa đơn
        /// </summary>
        Task<bool> UpdateInvoicePayment(int invoiceId, decimal paidAmount, string paymentMethod);

        /// <summary>
        /// Lấy thông tin thanh toán hóa đơn
        /// </summary>
        Task<InvoicePaymentInfoModel> GetInvoicePaymentInfo(int invoiceId);
    }
}