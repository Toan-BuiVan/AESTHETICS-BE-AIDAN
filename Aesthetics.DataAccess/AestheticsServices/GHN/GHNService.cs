using Aesthetics.Data.AestheticsInterfaces.GHN;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices.GHN
{
    public class GHNService : IGHNService
    {
        private readonly ILogger<GHNService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private string _ghnApiUrl;
        private string _ghnToken;

        public GHNService(
            ILogger<GHNService> logger,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
            _ghnApiUrl = _configuration["GHN:ApiUrl"];
            _ghnToken = _configuration["GHN:Token"];
        }

		public async Task<JsonDocument> GetProvincesAsync()
		{
			try
			{
				var url = $"{_ghnApiUrl}/master-data/province";
				var request = new HttpRequestMessage(HttpMethod.Post, url);
				request.Headers.Add("token", _ghnToken);

				var response = await _httpClient.SendAsync(request);
				response.EnsureSuccessStatusCode();

				var jsonContent = await response.Content.ReadAsStringAsync();
				var result = JsonDocument.Parse(jsonContent);

				_logger.LogInformation("Successfully retrieved GHN provinces");
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in GetProvincesAsync: {ex.Message}");
				throw;
			}
		}

		public async Task<JsonDocument> GetDistrictsAsync(int provinceId)
		{
			try
			{
				var url = $"{_ghnApiUrl}/master-data/district";
				var request = new HttpRequestMessage(HttpMethod.Post, url);
				request.Headers.Add("token", _ghnToken);

				var payload = new { province_id = provinceId };
				var jsonPayload = JsonSerializer.Serialize(payload);
				request.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

				var response = await _httpClient.SendAsync(request);
				response.EnsureSuccessStatusCode();

				var jsonContent = await response.Content.ReadAsStringAsync();
				var result = JsonDocument.Parse(jsonContent);

				_logger.LogInformation($"Successfully retrieved GHN districts for province: {provinceId}");
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in GetDistrictsAsync: {ex.Message}");
				throw;
			}
		}

		public async Task<JsonDocument> GetWardsAsync(int districtId)
		{
			try
			{
				var url = $"{_ghnApiUrl}/master-data/ward";
				var request = new HttpRequestMessage(HttpMethod.Post, url);
				request.Headers.Add("token", _ghnToken);

				var payload = new { district_id = districtId };
				var jsonPayload = JsonSerializer.Serialize(payload);
				request.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

				var response = await _httpClient.SendAsync(request);
				response.EnsureSuccessStatusCode();

				var jsonContent = await response.Content.ReadAsStringAsync();
				var result = JsonDocument.Parse(jsonContent);

				_logger.LogInformation($"Successfully retrieved GHN wards for district: {districtId}");
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in GetWardsAsync: {ex.Message}");
				throw;
			}
		}

		public async Task<JsonDocument> GetAvailableServicesAsync(int fromDistrict, int toDistrict, int shopId = 6387655)
		{
			try
			{
				var url = $"{_ghnApiUrl}/v2/shipping-order/available-services";
				var request = new HttpRequestMessage(HttpMethod.Post, url);
				request.Headers.Add("token", _ghnToken);

				var payload = new
				{
					shop_id = shopId,
					from_district = fromDistrict,
					to_district = toDistrict
				};
				var jsonPayload = JsonSerializer.Serialize(payload);
				request.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

				var response = await _httpClient.SendAsync(request);
				response.EnsureSuccessStatusCode();

				var jsonContent = await response.Content.ReadAsStringAsync();
				var result = JsonDocument.Parse(jsonContent);

				_logger.LogInformation($"Successfully retrieved available shipping services from district {fromDistrict} to {toDistrict}");
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in GetAvailableServicesAsync: {ex.Message}");
				throw;
			}
		}

		public async Task<JsonDocument> CalculateShippingFeeAsync(CalculateShippingFeeRequest request, int shopId = 6387655)
		{
			try
			{
				if (!request.ServiceId.HasValue && !request.ServiceTypeId.HasValue)
				{
					throw new ArgumentException("Ph?i cung c?p ServiceId ho?c ServiceTypeId");
				}

				var url = $"{_ghnApiUrl}/v2/shipping-order/fee";
				var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
				httpRequest.Headers.Add("token", _ghnToken);
				httpRequest.Headers.Add("shop_id", shopId.ToString());

				var payloadDict = new Dictionary<string, object>
				{
					{ "insurance_value", request.InsuranceValue },
					{ "to_ward_code", request.ToWardCode },
					{ "to_district_id", request.ToDistrictId },
					{ "from_district_id", request.FromDistrictId },
					{ "weight", request.Weight },
					{ "length", request.Length },
					{ "width", request.Width },
					{ "height", request.Height }
				};

				if (request.ServiceId.HasValue)
				{
					payloadDict["service_id"] = request.ServiceId.Value;
				}

				if (request.ServiceTypeId.HasValue)
				{
					payloadDict["service_type_id"] = request.ServiceTypeId.Value;
				}

				if (!string.IsNullOrWhiteSpace(request.Coupon))
				{
					payloadDict["coupon"] = request.Coupon;
				}

				var jsonPayload = JsonSerializer.Serialize(payloadDict);
				httpRequest.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

				var response = await _httpClient.SendAsync(httpRequest);
				response.EnsureSuccessStatusCode();

				var jsonContent = await response.Content.ReadAsStringAsync();
				var result = JsonDocument.Parse(jsonContent);

				_logger.LogInformation($"Successfully calculated shipping fee from district {request.FromDistrictId} to {request.ToDistrictId}");
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in CalculateShippingFeeAsync: {ex.Message}");
				throw;
			}
		}
	}
}