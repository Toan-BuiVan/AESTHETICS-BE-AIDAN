
using Aesthetics.DTO.NetCore.DataObject.Model.VnPay;

namespace ASP_NetCore_Aesthetics.Services.VnPaySevices
{
	public interface IVnPayService
	{
		string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
		PaymentResponseModel PaymentExecute(IQueryCollection collections);


		/// <summary>
		/// Tạo request hoàn tiền cho VNPay
		/// </summary>
		string CreateRefundUrl(RefundInformationModel model);

		/// <summary>
		/// Xử lý kết quả hoàn tiền từ VNPay
		/// </summary>
		PaymentResponseModel RefundExecute(IQueryCollection collections);
	}
}
