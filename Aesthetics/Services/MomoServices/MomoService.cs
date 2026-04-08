using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System.Security.Cryptography;
using System.Text;
using Aesthetics.DTO.NetCore.DataObject.Model.Momo;

namespace ASP_NetCore_Aesthetics.Services.MomoServices
{
	public class MomoService : IMomoService
	{
		private readonly IOptions<MomoOptionModel> _options;
		private readonly ILogger<MomoService> _logger;

		public MomoService(IOptions<MomoOptionModel> options, ILogger<MomoService> logger)
		{
			_options = options;
			_logger = logger;
		}

		public async Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model)
		{
			try
			{
				_logger.LogInformation("MOMO_CREATE_PAYMENT_START: Creating payment for OrderId: {OrderId}", model.OrderId);

				

				// 🟢 BUILD PAYLOAD
				model.OrderId = DateTime.UtcNow.Ticks.ToString();
				model.OrderInfo = "Khách hàng: " + model.FullName + ". Nội dung: " + model.OrderInfo;

				_logger.LogInformation("MOMO_BUILDING_RAW_DATA: OrderId={OrderId}, Amount={Amount}", model.OrderId, model.Amount);

				// ✅ BUILD RAW DATA - All values should be NOT NULL now
				var rawData =
					$"partnerCode={_options.Value.PartnerCode}" +
					$"&accessKey={_options.Value.AccessKey}" +
					$"&requestId={model.OrderId}" +
					$"&amount={model.Amount}" +
					$"&orderId={model.OrderId}" +
					$"&orderInfo={model.OrderInfo}" +
					$"&returnUrl={_options.Value.ReturnUrl}" +
					$"&notifyUrl={_options.Value.NotifyUrl}" +
					$"&extraData=";

				_logger.LogInformation("MOMO_RAW_DATA_BUILT: Length={Length}", rawData.Length);

				// 🟡 COMPUTE SIGNATURE
				var signature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

				_logger.LogInformation("MOMO_SIGNATURE_COMPUTED: Length={Length}", signature.Length);

				// 🟡 CREATE REQUEST
				var client = new RestClient(_options.Value.MomoApiUrl);
				var request = new RestRequest() { Method = Method.Post };
				request.AddHeader("Content-Type", "application/json; charset=UTF-8");

				var requestData = new
				{
					accessKey = _options.Value.AccessKey,
					partnerCode = _options.Value.PartnerCode,
					requestType = _options.Value.RequestType,
					notifyUrl = _options.Value.NotifyUrl,
					returnUrl = _options.Value.ReturnUrl,
					orderId = model.OrderId,
					amount = model.Amount.ToString(),
					orderInfo = model.OrderInfo,
					requestId = model.OrderId,
					extraData = "",
					signature = signature
				};

				var jsonPayload = JsonConvert.SerializeObject(requestData);
				request.AddParameter("application/json", jsonPayload, ParameterType.RequestBody);

				_logger.LogInformation("MOMO_SENDING_REQUEST: MomoApiUrl={MomoApiUrl}", _options.Value.MomoApiUrl);

				// 🟡 SEND REQUEST
				var response = await client.ExecuteAsync(request);

				_logger.LogInformation("MOMO_RESPONSE_RECEIVED: StatusCode={StatusCode}, Content={Content}", 
					response.StatusCode, response.Content);

				if (string.IsNullOrEmpty(response.Content))
				{
					_logger.LogError("❌ MOMO_EMPTY_RESPONSE: Response content is empty or null");
					throw new InvalidOperationException("Empty response from Momo API");
				}

				// 🟡 PARSE RESPONSE
				var momoResponse = JsonConvert.DeserializeObject<MomoCreatePaymentResponseModel>(response.Content);

				if (momoResponse == null)
				{
					_logger.LogError("❌ MOMO_RESPONSE_PARSE_FAILED: Could not deserialize response");
					throw new InvalidOperationException("Failed to parse Momo API response");
				}

				_logger.LogInformation("MOMO_CREATE_PAYMENT_SUCCESS: PayUrl={PayUrl}", momoResponse?.PayUrl ?? "NULL");

				return momoResponse;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ MOMO_CREATE_PAYMENT_EXCEPTION: Error creating Momo payment");
				throw;
			}
		}

		public MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection)
		{
			var amount = collection.First(s => s.Key == "amount").Value;
			var orderInfo = collection.First(s => s.Key == "orderInfo").Value;
			var orderId = collection.First(s => s.Key == "orderId").Value;

			return new MomoExecuteResponseModel()
			{
				Amount = amount,
				OrderId = orderId,
				OrderInfo = orderInfo
			};
		}

		private string ComputeHmacSha256(string message, string secretKey)
		{
			// 🔴 VALIDATION
			if (string.IsNullOrEmpty(message))
			{
				_logger.LogError("❌ HMAC_MESSAGE_NULL: Message is null or empty");
				throw new ArgumentNullException(nameof(message), "Message cannot be null or empty");
			}

			if (string.IsNullOrEmpty(secretKey))
			{
				_logger.LogError("❌ HMAC_SECRET_KEY_NULL: SecretKey is null or empty");
				throw new ArgumentNullException(nameof(secretKey), "Secret key cannot be null or empty");
			}

			try
			{
				var keyBytes = Encoding.UTF8.GetBytes(secretKey);
				var messageBytes = Encoding.UTF8.GetBytes(message);

				byte[] hashBytes;

				using (var hmac = new HMACSHA256(keyBytes))
				{
					hashBytes = hmac.ComputeHash(messageBytes);
				}

				var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

				_logger.LogInformation("HMAC_SHA256_COMPUTED: Hash={Hash}", hashString);

				return hashString;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ HMAC_COMPUTATION_ERROR: Error computing HMAC SHA256");
				throw;
			}
		}
	}
}
