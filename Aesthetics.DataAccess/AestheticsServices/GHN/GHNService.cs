using Aesthetics.Data.AestheticsInterfaces.GHN;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
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
		private readonly string _ghnToken;
		private readonly string _ghnApiUrl;
		private readonly HttpClient _httpClient;
		private readonly ILogger<GHNService> _logger;
		private readonly IConfiguration _configuration;
		private readonly IInvoiceRepository _invoiceRepository;
		private readonly ICustomerRepository _customerRepository;
		private readonly IAddressInfoRepository _addressInfoRepository;
		private readonly IInvoiceDetailsRepository _invoiceDetailsRepository;


		public GHNService(
            ILogger<GHNService> logger,
            HttpClient httpClient,
            IConfiguration configuration,
			IInvoiceRepository invoiceRepository,
			ICustomerRepository customerRepository,
			IAddressInfoRepository addressInfoRepository,
			IInvoiceDetailsRepository invoiceDetailsRepository
			)
        {
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
			_invoiceRepository = invoiceRepository;
			_customerRepository = customerRepository;
			_ghnToken = _configuration["GHN:Token"];
			_ghnApiUrl = _configuration["GHN:ApiUrl"];
			_addressInfoRepository = addressInfoRepository;
			_invoiceDetailsRepository = invoiceDetailsRepository;
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

		public async Task<JsonDocument> CalculateShippingFeesAsync(CreateShippingOrderRequest createShippingOrder, int fromDistrict = 2194, int shopId = 6387655)
		{
			try
			{
				if (createShippingOrder == null || createShippingOrder.InvoiceIds.Count == 0)
				{
					_logger.LogWarning("No invoice IDs provided in CalculateShippingFeesAsync");
					throw new InvalidOperationException("Vui lòng cung cấp ít nhất một hóa đơn");
				}

				var shippingFees = new List<object>();
				var processedCustomers = new Dictionary<int, int?>();
				var lightWeightServiceId = 53321;

				foreach (var invoiceId in createShippingOrder.InvoiceIds)
				{
					// Lấy thông tin hóa đơn
					var invoice = await _invoiceRepository.GetById(invoiceId);

					if (invoice == null)
					{
						_logger.LogWarning($"Invoice with ID {invoiceId} not found");
						throw new InvalidOperationException($"Không tìm thấy hóa đơn với ID {invoiceId}");
					}

					if (!invoice.CustomerId.HasValue)
					{
						_logger.LogWarning($"Invoice {invoiceId} does not have a customer ID");
						throw new InvalidOperationException($"Hóa đơn {invoiceId} không có khách hàng");
					}

					var customerId = invoice.CustomerId.Value;

					// Lấy địa chỉ mặc định của khách hàng
					var addressesCollection = await _addressInfoRepository.FindByPredicate(
						a => a.CustomerId == customerId && a.IsDefault == true);
					var customerDefaultAddress = addressesCollection?.FirstOrDefault();

					if (customerDefaultAddress == null)
					{
						_logger.LogWarning($"No default address found for customer {customerId}");
						throw new InvalidOperationException($"Không tìm thấy địa chỉ mặc định cho khách hàng {customerId}");
					}

					if (!customerDefaultAddress.DistrictId.HasValue || string.IsNullOrWhiteSpace(customerDefaultAddress.WardCode))
					{
						_logger.LogWarning($"Default address for customer {customerId} missing district or ward code");
						throw new InvalidOperationException($"Địa chỉ mặc định không có đầy đủ thông tin quận/huyện hoặc phường/xã");
					}

					// Lấy available services nếu chưa có cho customer này
					if (!processedCustomers.ContainsKey(customerId))
					{
						var availableServicesUrl = $"{_ghnApiUrl}/v2/shipping-order/available-services";
						var availableServicesRequest = new HttpRequestMessage(HttpMethod.Post, availableServicesUrl);
						availableServicesRequest.Headers.Add("token", _ghnToken);

						var availableServicesPayload = new
						{
							shop_id = shopId,
							from_district = fromDistrict,
							to_district = customerDefaultAddress.DistrictId.Value
						};
						var availableServicesJsonPayload = JsonSerializer.Serialize(availableServicesPayload);
						availableServicesRequest.Content = new StringContent(availableServicesJsonPayload, System.Text.Encoding.UTF8, "application/json");

						var availableServicesResponse = await _httpClient.SendAsync(availableServicesRequest);
						availableServicesResponse.EnsureSuccessStatusCode();

						var availableServicesJsonContent = await availableServicesResponse.Content.ReadAsStringAsync();
						var availableServicesResult = JsonDocument.Parse(availableServicesJsonContent);

						// Lấy service_id của "Hàng nhẹ" (lightweight)
						using (var doc = availableServicesResult)
						{
							var root = doc.RootElement;
							var dataElement = root.GetProperty("data");
							var lightWeightService = dataElement.EnumerateArray()
								.FirstOrDefault(s => s.GetProperty("short_name").GetString() == "Hàng nhẹ");

							if (lightWeightService.ValueKind != System.Text.Json.JsonValueKind.Undefined)
							{
								lightWeightServiceId = lightWeightService.GetProperty("service_id").GetInt32();
								processedCustomers[customerId] = lightWeightServiceId;
							}
							else
							{
								_logger.LogWarning($"Lightweight service (Hàng nhẹ) not found for customer {customerId}");
								throw new InvalidOperationException($"Không tìm thấy dịch vụ vận chuyển hàng nhẹ cho khách hàng {customerId}");
							}
						}

						_logger.LogInformation($"Successfully retrieved available shipping services for customer {customerId} with lightweight service ID {lightWeightServiceId}");
					}
					else
					{
						lightWeightServiceId = processedCustomers[customerId] ?? 53321;
					}

					// Tính phí vận chuyển
					var insuranceValue = (int)(invoice.FinalPrice ?? 0);
					int codAmount = 0;

					// Nếu chưa thanh toán, tính tiền cần thu COD
					if (invoice.Status != "DaThanhToan")
					{
						// Thu tối đa 300k, phần còn lại khách thanh toán online
						codAmount = Math.Min(insuranceValue, 300000);
					}

					var feeUrl = $"{_ghnApiUrl}/v2/shipping-order/fee";
					var feeRequest = new HttpRequestMessage(HttpMethod.Post, feeUrl);
					feeRequest.Headers.Add("token", _ghnToken);
					feeRequest.Headers.Add("shop_id", shopId.ToString());

					var feePayload = new Dictionary<string, object>
					{
						{ "insurance_value", insuranceValue },
						{ "to_ward_code", customerDefaultAddress.WardCode },
						{ "to_district_id", customerDefaultAddress.DistrictId.Value },
						{ "from_district_id", fromDistrict },
						{ "weight", 500 },
						{ "length", 15 },
						{ "width", 15 },
						{ "height", 15 },
						{ "service_id", lightWeightServiceId }
					};

					var feeJsonPayload = JsonSerializer.Serialize(feePayload);
					feeRequest.Content = new StringContent(feeJsonPayload, System.Text.Encoding.UTF8, "application/json");

					var feeResponse = await _httpClient.SendAsync(feeRequest);
					feeResponse.EnsureSuccessStatusCode();

					var feeJsonContent = await feeResponse.Content.ReadAsStringAsync();
					var feeResult = JsonDocument.Parse(feeJsonContent);

					shippingFees.Add(new
					{
						invoiceId = invoiceId,
						customerId = customerId,
						serviceId = lightWeightServiceId,
						shippingFee = feeResult
					});

					_logger.LogInformation($"Successfully calculated shipping fee for invoice {invoiceId} with lightweight service (ID: {lightWeightServiceId})");
				}

				// Trả về kết quả cho tất cả các hóa đơn
				var finalResult = JsonDocument.Parse(JsonSerializer.Serialize(new { data = shippingFees }));
				return finalResult;
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in CalculateShippingFeesAsync: {ex.Message}");
				throw;
			}
		}

		public async Task<JsonDocument> CreateShippingOrdersAsync(CreateShippingOrderRequest createShippingOrder, int fromDistrict = 2194, int shopId = 6387655)
		{
			try
			{
				if (createShippingOrder == null || createShippingOrder.InvoiceIds.Count == 0)
				{
					_logger.LogWarning("No invoice IDs provided in CreateShippingOrdersAsync");
					throw new InvalidOperationException("Vui lòng cung cấp ít nhất một hóa đơn");
				}

				var createdOrders = new List<object>();
				var processedCustomers = new Dictionary<int, int?>();
				var lightWeightServiceId = 53321;

				foreach (var invoiceId in createShippingOrder.InvoiceIds)
				{
					try
					{
						// Lấy thông tin hóa đơn
						var invoice = await _invoiceRepository.GetById(invoiceId);

						if (invoice == null)
						{
							_logger.LogWarning($"Invoice with ID {invoiceId} not found");
							createdOrders.Add(new { invoiceId = invoiceId, success = false, error = $"Không tìm thấy hóa đơn với ID {invoiceId}" });
							continue;
						}

						if (invoice.IsDelivered == true)
						{
							_logger.LogWarning($"Invoice {invoiceId} has already been delivered (IsDelivered = true)");
							createdOrders.Add(new { invoiceId = invoiceId, success = false, error = $"Hóa đơn {invoiceId} đã được giao rồi, không thể tạo đơn hàng mới" });
							continue;
						}

						if (!invoice.CustomerId.HasValue)
						{
							_logger.LogWarning($"Invoice {invoiceId} does not have a customer ID");
							createdOrders.Add(new { invoiceId = invoiceId, success = false, error = $"Hóa đơn {invoiceId} không có khách hàng" });
							continue;
						}


						// Lấy thông tin khách hàng từ Customer
						var customer = await _customerRepository.GetById(invoice.CustomerId.Value);
						if (customer == null)
						{
							_logger.LogWarning($"Customer with ID {invoice.CustomerId.Value} not found");
							createdOrders.Add(new { invoiceId = invoiceId, success = false, error = $"Không tìm thấy thông tin khách hàng {invoice.CustomerId.Value}" });
							continue;
						}

						// Lấy invoice details để kiểm tra ProductId
						var invoiceDetails = await _invoiceDetailsRepository.FindByPredicate(d => d.InvoiceId == invoiceId);

						// Kiểm tra xem có chi tiết hóa đơn nào có ProductId không
						if (invoiceDetails == null || !invoiceDetails.Any())
						{
							_logger.LogWarning($"Invoice {invoiceId} has no details");
							createdOrders.Add(new { invoiceId = invoiceId, success = false, error = "Hóa đơn không có chi tiết sản phẩm/dịch vụ" });
							continue;
						}

						// Kiểm tra xem tất cả chi tiết có ProductId hay không
						var detailsWithoutProduct = invoiceDetails.Where(d => !d.ProductId.HasValue).ToList();
						if (detailsWithoutProduct.Any())
						{
							_logger.LogWarning($"Invoice {invoiceId} has details without ProductId");
							createdOrders.Add(new { invoiceId = invoiceId, success = false, error = "Hóa đơn có chi tiết không có ProductId, không thể tạo đơn hàng" });
							continue;
						}

						// Lấy địa chỉ mặc định của khách hàng
						var addressesCollection = await _addressInfoRepository.FindByPredicate(
							a => a.CustomerId == invoice.CustomerId.Value && a.IsDefault == true);
						var customerDefaultAddress = addressesCollection?.FirstOrDefault();

						if (customerDefaultAddress == null)
						{
							_logger.LogWarning($"No default address found for customer {invoice.CustomerId.Value}");
							createdOrders.Add(new { invoiceId = invoiceId, success = false, error = $"Không tìm thấy địa chỉ mặc định cho khách hàng {invoice.CustomerId.Value}" });
							continue;
						}

						if (!customerDefaultAddress.DistrictId.HasValue || string.IsNullOrWhiteSpace(customerDefaultAddress.WardCode))
						{
							_logger.LogWarning($"Default address for customer {invoice.CustomerId.Value} missing district or ward code");
							createdOrders.Add(new { invoiceId = invoiceId, success = false, error = "Địa chỉ mặc định không có đầy đủ thông tin quận/huyện hoặc phường/xã" });
							continue;
						}

						// Lấy available services nếu chưa có cho customer này
						if (!processedCustomers.ContainsKey(invoice.CustomerId.Value))
						{
							var availableServicesUrl = $"{_ghnApiUrl}/v2/shipping-order/available-services";
							var availableServicesRequest = new HttpRequestMessage(HttpMethod.Post, availableServicesUrl);
							availableServicesRequest.Headers.Add("token", _ghnToken);

							var availableServicesPayload = new
							{
								shop_id = shopId,
								from_district = fromDistrict,
								to_district = customerDefaultAddress.DistrictId.Value
							};
							var availableServicesJsonPayload = JsonSerializer.Serialize(availableServicesPayload);
							availableServicesRequest.Content = new StringContent(availableServicesJsonPayload, System.Text.Encoding.UTF8, "application/json");

							var availableServicesResponse = await _httpClient.SendAsync(availableServicesRequest);
							if (!availableServicesResponse.IsSuccessStatusCode)
							{
								_logger.LogWarning($"Failed to get available services for customer {invoice.CustomerId.Value}");
								createdOrders.Add(new { invoiceId = invoiceId, success = false, error = "Không thể lấy danh sách dịch vụ vận chuyển" });
								continue;
							}

							var availableServicesJsonContent = await availableServicesResponse.Content.ReadAsStringAsync();
							var availableServicesResult = JsonDocument.Parse(availableServicesJsonContent);

							// Lấy service_id của "Hàng nhẹ" (lightweight)
							using (var doc = availableServicesResult)
							{
								var root = doc.RootElement;
								if (root.TryGetProperty("data", out var dataElement))
								{
									var lightWeightService = dataElement.EnumerateArray()
										.FirstOrDefault(s => s.TryGetProperty("short_name", out var name) &&
											name.GetString() == "Hàng nhẹ");

									if (lightWeightService.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
										lightWeightService.TryGetProperty("service_id", out var serviceIdElement))
									{
										lightWeightServiceId = serviceIdElement.GetInt32();
										processedCustomers[invoice.CustomerId.Value] = lightWeightServiceId;
									}
									else
									{
										_logger.LogWarning($"Lightweight service (Hàng nhẹ) not found for customer {invoice.CustomerId.Value}");
										processedCustomers[invoice.CustomerId.Value] = null;
										createdOrders.Add(new { invoiceId = invoiceId, success = false, error = "Không tìm thấy dịch vụ vận chuyển hàng nhẹ" });
										continue;
									}
								}
								else
								{
									_logger.LogWarning($"Invalid response structure from available-services API for customer {invoice.CustomerId.Value}");
									createdOrders.Add(new { invoiceId = invoiceId, success = false, error = "Phản hồi từ API không hợp lệ" });
									continue;
								}
							}

							_logger.LogInformation($"Successfully retrieved available shipping services for customer {invoice.CustomerId.Value} with lightweight service ID {lightWeightServiceId}");
						}
						else
						{
							lightWeightServiceId = processedCustomers[invoice.CustomerId.Value] ?? 53321;
							if (processedCustomers[invoice.CustomerId.Value] == null)
							{
								createdOrders.Add(new { invoiceId = invoiceId, success = false, error = "Không có dịch vụ vận chuyển khả dụng cho khách hàng này" });
								continue;
							}
						}

						// Xây dựng danh sách sản phẩm từ invoice details (chỉ những cái có ProductId)
						var items = new List<object>();
						int totalWeight = 0;

						foreach (var detail in invoiceDetails.Where(d => d.ProductId.HasValue))
						{
							var productName = detail.Product?.ProductName ?? $"Sản phẩm {detail.ProductId}";
							var quantity = detail.Quantity ?? 1;
							var price = (int)(detail.Price ?? 0);
							var itemWeight = 200; // mặc định 200g per item

							items.Add(new
							{
								name = productName,
								code = detail.ProductId.ToString(),
								quantity = quantity,
								price = price,
								length = 12,
								width = 12,
								height = 12,
								weight = itemWeight,
								category = new { level1 = "Sản phẩm" }
							});

							totalWeight += itemWeight * quantity;
						}

						if (items.Count == 0)
						{
							_logger.LogWarning($"Invoice {invoiceId} has no details with ProductId");
							createdOrders.Add(new { invoiceId = invoiceId, success = false, error = "Không có sản phẩm nào trong hóa đơn để tạo đơn hàng" });
							continue;
						}

						// Tạo đơn hàng vận chuyển
						var createOrderUrl = $"{_ghnApiUrl}/v2/shipping-order/create";
						var createOrderRequest = new HttpRequestMessage(HttpMethod.Post, createOrderUrl);
						createOrderRequest.Headers.Add("token", _ghnToken);
						createOrderRequest.Headers.Add("shop_id", shopId.ToString());

						if (totalWeight == 0)
							totalWeight = 500; 

						var insuranceValue = (int)(invoice.FinalPrice ?? 0);
						var limitedInsuranceValue = Math.Min(insuranceValue, 5000000);

						int codAmount = 0;
						if (invoice.Status != "DaThanhToan")
						{
							codAmount = Math.Min(insuranceValue, 50000000);
						}

						if (totalWeight > 50000)
						{
							_logger.LogWarning($"Invoice {invoiceId}: Weight {totalWeight}g exceeds GHN limit of 50,000g");
							createdOrders.Add(new { invoiceId = invoiceId, success = false, error = "Trọng lượng vượt giới hạn của GHN (50kg)" });
							continue;
						}

						var createOrderPayload = new
						{
							payment_type_id = 2,
							note = $"Đơn hàng từ hệ thống Aesthetics - Hóa đơn #{invoiceId}",
							required_note = "CHOXEMHANGKHONGTHU",
							from_name = "Aesthetics",
							from_phone = "0332190444",
							from_address = "Quang Hưng, Phù Cừ, Hưng Yên",
							from_ward_name = "Quang Hưng",
							from_district_name = "Phù Cừ",
							from_province_name = "Hưng Yên",
							return_phone = "0332190444",
							return_address = "39 NTT",
							return_district_id = 2194,
							return_ward_code = "220710",  
							client_order_code = $"INV-{invoiceId}-{DateTime.Now:yyyyMMddHHmmss}",
							to_name = customer.FullName ?? "Khách hàng",
							to_phone = customer.Phone ?? "",
							to_address = customerDefaultAddress.DetailAddress ?? "Địa chỉ giao hàng",
							to_ward_code = customerDefaultAddress.WardCode,
							to_district_id = customerDefaultAddress.DistrictId.Value,
							cod_amount = codAmount,
							content = $"Hóa đơn #{invoiceId}",
							weight = totalWeight,
							length = 15,
							width = 15,
							height = 15,
							insurance_value = limitedInsuranceValue,
							service_id = lightWeightServiceId,
							service_type_id = 2,
							coupon = (object)null,
							pick_shift = new[] { 2 },
							items = items
						};

						var createOrderJsonPayload = JsonSerializer.Serialize(createOrderPayload);
						_logger.LogInformation($"Invoice {invoiceId}: GHN Payload =\n{createOrderJsonPayload}");

						createOrderRequest.Content = new StringContent(createOrderJsonPayload, System.Text.Encoding.UTF8, "application/json");

						var createOrderResponse = await _httpClient.SendAsync(createOrderRequest);

						if (!createOrderResponse.IsSuccessStatusCode)
						{
							var errorContent = await createOrderResponse.Content.ReadAsStringAsync();
							_logger.LogError($"Failed to create shipping order for invoice {invoiceId}. Status: {createOrderResponse.StatusCode}, Error: {errorContent}");
							createdOrders.Add(new
							{
								invoiceId = invoiceId,
								success = false,
								error = $"Lỗi API GHN: {createOrderResponse.StatusCode}",
								details = errorContent
							});
							continue;
						}

						var createOrderJsonContent = await createOrderResponse.Content.ReadAsStringAsync();
						var createOrderResult = JsonDocument.Parse(createOrderJsonContent);
						invoice.IsDelivered = true;
						var updateInvoiceResult = await _invoiceRepository.UpdateEntity(invoice);

						createdOrders.Add(new
						{
							invoiceId = invoiceId,
							success = true,
							shippingOrderResponse = createOrderResult,
							message = $"Tạo đơn hàng vận chuyển thành công cho hóa đơn {invoiceId}"
						});

						_logger.LogInformation($"Successfully created shipping order for invoice {invoiceId}");
					}
					catch (Exception ex)
					{
						_logger.LogError($"Error processing invoice {invoiceId}: {ex.Message}");
						createdOrders.Add(new
						{
							invoiceId = invoiceId,
							success = false,
							error = ex.Message
						});
					}
				}

				var finalResult = JsonDocument.Parse(JsonSerializer.Serialize(new
				{
					data = createdOrders,
					totalCount = createdOrders.Count,
					successCount = createdOrders.Count(o => (bool)((dynamic)o).success),
					failureCount = createdOrders.Count - createdOrders.Count(o => (bool)((dynamic)o).success)
				}));

				return finalResult;
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in CreateShippingOrdersAsync: {ex.Message}");
				throw;
			}
		}

		//public async Task<JsonDocument> GetAvailableServicesAsync(int customerId, int fromDistrict, int shopId = 6387655)
		//{
		//	try
		//	{
		//		var addressesCollection = await _addressInfoRepository.FindByPredicate(a => a.CustomerId == customerId && a.IsDefault == true);
		//		var customerDefaultAddress = addressesCollection?.FirstOrDefault();

		//		if (customerDefaultAddress == null)
		//		{
		//			_logger.LogWarning($"No default address found for customer {customerId}");
		//			throw new InvalidOperationException($"Không tìm thấy địa chỉ mặc định cho khách hàng {customerId}");
		//		}

		//		var toDistrict = customerDefaultAddress.DistrictId;

		//		if (!toDistrict.HasValue)
		//		{
		//			_logger.LogWarning($"Default address for customer {customerId} does not have a district");
		//			throw new InvalidOperationException($"Địa chỉ mặc định của khách hàng {customerId} không có thông tin quận/huyện");
		//		}

		//		var url = $"{_ghnApiUrl}/v2/shipping-order/available-services";
		//		var request = new HttpRequestMessage(HttpMethod.Post, url);
		//		request.Headers.Add("token", _ghnToken);

		//		var payload = new
		//		{
		//			shop_id = shopId,
		//			from_district = fromDistrict,
		//			to_district = toDistrict.Value
		//		};
		//		var jsonPayload = JsonSerializer.Serialize(payload);
		//		request.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

		//		var response = await _httpClient.SendAsync(request);
		//		response.EnsureSuccessStatusCode();

		//		var jsonContent = await response.Content.ReadAsStringAsync();
		//		var result = JsonDocument.Parse(jsonContent);

		//		_logger.LogInformation($"Successfully retrieved available shipping services from district {fromDistrict} to customer {customerId}'s district {toDistrict}");
		//		return result;
		//	}
		//	catch (Exception ex)
		//	{
		//		_logger.LogError($"Error in GetAvailableServicesAsync for customer {customerId}: {ex.Message}");
		//		throw;
		//	}
		//}

		//public async Task<JsonDocument> CalculateShippingFeeAsync(CreateShippingOrderRequest request, int shopId = 6387655)
		//{
		//	try
		//	{
		//		if (request?.InvoiceIds == null || request.InvoiceIds.Count == 0)
		//		{
		//			_logger.LogWarning("No invoice IDs provided in CalculateShippingFeeAsync");
		//			throw new InvalidOperationException("Vui lòng cung cấp ít nhất một hóa đơn");
		//		}

		//		if (!request.ServiceId.HasValue)
		//		{
		//			_logger.LogWarning("Service ID is required in CalculateShippingFeeAsync");
		//			throw new InvalidOperationException("Vui lòng chọn dịch vụ vận chuyển");
		//		}

		//		var shippingFees = new List<object>();

		//		foreach (var invoiceId in request.InvoiceIds)
		//		{
		//			// Lấy thông tin hóa đơn
		//			var invoice = await _invoiceRepository.GetById(invoiceId);

		//			if (invoice == null)
		//			{
		//				_logger.LogWarning($"Invoice with ID {invoiceId} not found");
		//				throw new InvalidOperationException($"Không tìm thấy hóa đơn với ID {invoiceId}");
		//			}

		//			if (!invoice.CustomerId.HasValue)
		//			{
		//				_logger.LogWarning($"Invoice {invoiceId} does not have a customer ID");
		//				throw new InvalidOperationException($"Hóa đơn {invoiceId} không có khách hàng");
		//			}

		//			// Lấy địa chỉ mặc định của khách hàng
		//			var addressesCollection = await _addressInfoRepository.FindByPredicate(
		//				a => a.CustomerId == invoice.CustomerId && a.IsDefault == true);
		//			var customerDefaultAddress = addressesCollection?.FirstOrDefault();

		//			if (customerDefaultAddress == null)
		//			{
		//				_logger.LogWarning($"No default address found for customer {invoice.CustomerId}");
		//				throw new InvalidOperationException($"Không tìm thấy địa chỉ mặc định cho khách hàng {invoice.CustomerId}");
		//			}

		//			if (!customerDefaultAddress.DistrictId.HasValue || string.IsNullOrWhiteSpace(customerDefaultAddress.WardCode))
		//			{
		//				_logger.LogWarning($"Default address for customer {invoice.CustomerId} missing district or ward code");
		//				throw new InvalidOperationException($"Địa chỉ mặc định không có đầy đủ thông tin quận/huyện hoặc phường/xã");
		//			}

		//			// Insurance value từ giá trị hóa đơn (FinalPrice)
		//			var insuranceValue = (int)(invoice.FinalPrice ?? 0);

		//			var url = $"{_ghnApiUrl}/v2/shipping-order/fee";
		//			var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
		//			httpRequest.Headers.Add("token", _ghnToken);
		//			httpRequest.Headers.Add("shop_id", shopId.ToString());

		//			var payloadDict = new Dictionary<string, object>
		//		{
		//			{ "insurance_value", insuranceValue },
		//			{ "to_ward_code", customerDefaultAddress.WardCode },
		//			{ "to_district_id", customerDefaultAddress.DistrictId.Value },
		//			{ "from_district_id", 2194 },
		//			{ "weight", 500 },
		//			{ "length", 15 },
		//			{ "width", 15 },
		//			{ "height", 15 },
		//			{ "service_id", request.ServiceId.Value }
		//		};

		//			var jsonPayload = JsonSerializer.Serialize(payloadDict);
		//			httpRequest.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

		//			var response = await _httpClient.SendAsync(httpRequest);
		//			response.EnsureSuccessStatusCode();

		//			var jsonContent = await response.Content.ReadAsStringAsync();
		//			var result = JsonDocument.Parse(jsonContent);

		//			shippingFees.Add(new
		//			{
		//				invoiceId = invoiceId,
		//				shippingFee = result
		//			});

		//			_logger.LogInformation($"Successfully calculated shipping fee for invoice {invoiceId} with service {request.ServiceId}");
		//		}

		//		// Trả về kết quả cho tất cả các hóa đơn
		//		var finalResult = JsonDocument.Parse(JsonSerializer.Serialize(new { data = shippingFees }));
		//		return finalResult;
		//	}
		//	catch (Exception ex)
		//	{
		//		_logger.LogError($"Error in CalculateShippingFeeAsync: {ex.Message}");
		//		throw;
		//	}
		//}
	}
}