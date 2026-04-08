using ASP_NetCore_Aesthetics.Library;
using Aesthetics.DTO.NetCore.DataObject.Model.VnPay;
using Microsoft.Extensions.Logging;
using System;

namespace ASP_NetCore_Aesthetics.Services.VnPaySevices
{
	public class VnPayService : IVnPayService
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<VnPayService> _logger;

		public VnPayService(IConfiguration configuration, ILogger<VnPayService> logger)
		{
			_configuration = configuration;
			_logger = logger;
		}

		public string CreatePaymentUrl(PaymentInformationModel model, HttpContext context)
		{
			try
			{
				_logger.LogInformation("VNPAY_CREATE_PAYMENT_URL_START: Tạo URL thanh toán VNPay - OrderId: {OrderId}, Amount: {Amount:C}",
					model.OrderID, model.Amount);

				var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(_configuration["TimeZoneId"]);
				var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
				var tick = DateTime.Now.Ticks.ToString();
				var pay = new VnPayLibrary();
				var urlCallBack = _configuration["Vnpay:PaymentBackReturnUrl"];

				pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]);
				pay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"]);
				pay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]);
				pay.AddRequestData("vnp_Amount", ((decimal)model.Amount * 100).ToString());
				pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
				pay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"]);
				pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));
				pay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"]);
				pay.AddRequestData("vnp_OrderInfo", $"OrderID:{model.OrderID}|{model.Name}|{model.OrderDescription}|{model.Amount}");
				pay.AddRequestData("vnp_OrderType", model.OrderID);
				pay.AddRequestData("vnp_ReturnUrl", urlCallBack);
				pay.AddRequestData("vnp_TxnRef", tick);

				var paymentUrl = pay.CreateRequestUrl(_configuration["Vnpay:BaseUrl"], _configuration["Vnpay:HashSecret"]);

				_logger.LogInformation("VNPAY_CREATE_PAYMENT_URL_SUCCESS: URL thanh toán VNPay được tạo thành công - OrderId: {OrderId}, PaymentUrl: {PaymentUrl}",
					model.OrderID, paymentUrl);

				return paymentUrl;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "VNPAY_CREATE_PAYMENT_URL_EXCEPTION: Lỗi khi tạo URL thanh toán VNPay - OrderId: {OrderId}",
					model.OrderID);
				throw;
			}
		}

		public PaymentResponseModel PaymentExecute(IQueryCollection collections)
		{
			try
			{
				_logger.LogInformation("VNPAY_PAYMENT_EXECUTE_START: Xử lý kết quả thanh toán VNPay");

				var pay = new VnPayLibrary();
				var response = pay.GetFullResponseData(collections, _configuration["Vnpay:HashSecret"]);

				_logger.LogInformation("VNPAY_PAYMENT_EXECUTE_SUCCESS: Xử lý kết quả thanh toán thành công - ResponseCode: {ResponseCode}",
					response?.VnPayResponseCode);

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "VNPAY_PAYMENT_EXECUTE_EXCEPTION: Lỗi khi xử lý kết quả thanh toán VNPay");
				throw;
			}
		}
	}
}
