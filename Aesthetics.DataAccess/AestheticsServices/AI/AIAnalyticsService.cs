using Aesthetics.Data.AestheticsInterfaces.AI;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel.AI;
using Aesthetics.Entities.Models.ResponseModel.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices.AI
{
	public class AIAnalyticsService : IAIAnalyticsService
	{
		private readonly ILogger<AIAnalyticsService> _logger;
		private readonly IServiceRepository _serviceRepository;
		private readonly IAppointmentRepositoty _appointmentRepository;
		private readonly IProductRepository _productRepository;
		private readonly ITreatmentPlanRepository _treatmentPlanRepository;
		private readonly IInvoiceDetailsRepository _invoiceDetailsRepository;
		private readonly ICartProductRepository _cartProductRepository;
		private readonly IStaffRepository _staffRepository;
		private readonly ITreatmentSessionRepository _treatmentSessionRepository;
		private readonly ILLMService _llmService;

		public AIAnalyticsService(
			ILogger<AIAnalyticsService> logger,
			IServiceRepository serviceRepository,
			IAppointmentRepositoty appointmentRepository,
			IProductRepository productRepository,
			ITreatmentPlanRepository treatmentPlanRepository,
			IInvoiceDetailsRepository invoiceDetailsRepository,
			ICartProductRepository cartProductRepository,
			IStaffRepository staffRepository,
			ITreatmentSessionRepository treatmentSessionRepository,
			ILLMService llmService)
		{
			_logger = logger;
			_serviceRepository = serviceRepository;
			_appointmentRepository = appointmentRepository;
			_productRepository = productRepository;
			_treatmentPlanRepository = treatmentPlanRepository;
			_invoiceDetailsRepository = invoiceDetailsRepository;
			_cartProductRepository = cartProductRepository;
			_staffRepository = staffRepository;
			_treatmentSessionRepository = treatmentSessionRepository;
			_llmService = llmService;
		}

		/// <summary>Bài 4: Lấy dịch vụ nhiều người dùng nhất</summary>
		public async Task<AIMostUsedServiceResponse> GetMostUsedServiceAsync()
		{
			try
			{
				_logger.LogInformation("GET_MOST_USED_SERVICE: Processing");

				var response = new AIMostUsedServiceResponse();

				// Lấy tất cả dịch vụ
				var services = await _serviceRepository.FindByPredicate(x => !x.DeleteStatus);

				if (!services.Any())
				{
					response.Success = false;
					response.Message = "Không tìm thấy dịch vụ nào";
					return response;
				}

				// Đếm số lượng lịch hẹn cho mỗi dịch vụ
				var serviceUsageList = new List<(int ServiceId, string ServiceName, decimal Price, int Count)>();

				foreach (var service in services)
				{
					var appointmentCount = (await _appointmentRepository.FindByPredicate(x =>
						x.ServiceId == service.Id &&
						!x.DeleteStatus)).Count();

					serviceUsageList.Add((service.Id, service.ServiceName, service.Price ?? 0, appointmentCount));
				}

				// Sắp xếp và lấy dịch vụ phổ biến nhất
				var mostUsedService = serviceUsageList.OrderByDescending(x => x.Count).FirstOrDefault();

				if (mostUsedService.Count == 0)
				{
					response.Success = false;
					response.Message = "Không có dịch vụ nào được sử dụng";
					return response;
				}

				response.ServiceId = mostUsedService.ServiceId;
				response.ServiceName = mostUsedService.ServiceName;
				response.Price = mostUsedService.Price;
				response.UserCount = mostUsedService.Count;
				response.Success = true;
				response.Message = $"Dịch vụ {mostUsedService.ServiceName} được sử dụng nhiều nhất với {mostUsedService.Count} lịch hẹn";

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_MOST_USED_SERVICE_ERROR: Exception occurred");
				return new AIMostUsedServiceResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}"
				};
			}
		}

		/// <summary>Bài 5: Lấy bác sĩ tốt nhất của liệu trình</summary>
		public async Task<AIBestDoctorResponse> GetBestDoctorForTreatmentPlanAsync(int treatmentPlanId)
		{
			try
			{
				_logger.LogInformation("GET_BEST_DOCTOR_FOR_PLAN: treatmentPlanId={PlanId}", treatmentPlanId);

				var response = new AIBestDoctorResponse();

				// Kiểm tra liệu trình
				var treatmentPlan = await _treatmentPlanRepository.GetById(treatmentPlanId);
				if (treatmentPlan == null || treatmentPlan.DeleteStatus)
				{
					response.Success = false;
					response.Message = "Liệu trình không tồn tại";
					return response;
				}

				response.TreatmentPlanName = treatmentPlan.PlanName;

				// Lấy tất cả lịch hẹn liên quan đến liệu trình này
				var appointments = await _appointmentRepository.FindByPredicate(x =>
					x.CustomerTreatmentPlanId != null &&
					!x.DeleteStatus);

				// Nhóm theo bác sĩ
				var doctorAppointments = appointments
					.GroupBy(x => x.StaffId)
					.Select(g => new { StaffId = g.Key, Count = g.Count() })
					.OrderByDescending(x => x.Count)
					.FirstOrDefault();

				if (doctorAppointments == null || doctorAppointments.Count == 0)
				{
					response.Success = false;
					response.Message = "Không tìm thấy bác sĩ nào cho liệu trình này";
					return response;
				}

				response.StaffId = doctorAppointments.StaffId.Value;
				response.AppointmentCount = doctorAppointments.Count;
				response.Success = true;
				response.Message = $"Bác sĩ có ID {doctorAppointments.StaffId} có {doctorAppointments.Count} lịch hẹn nhiều nhất";

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_BEST_DOCTOR_FOR_PLAN_ERROR: Exception occurred");
				return new AIBestDoctorResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}"
				};
			}
		}

		/// <summary>Lấy bác sĩ tốt nhất (nhiều lịch đặt nhất) của một dịch vụ</summary>
		public async Task<AIBestDoctorResponse> GetBestDoctorForServiceAsync(int serviceId)
		{
			try
			{
				_logger.LogInformation("GET_BEST_DOCTOR_FOR_SERVICE: serviceId={ServiceId}", serviceId);

				var response = new AIBestDoctorResponse();

				// Kiểm tra dịch vụ
				var service = await _serviceRepository.GetById(serviceId);
				if (service == null || service.DeleteStatus)
				{
					response.Success = false;
					response.Message = "Dịch vụ không tồn tại";
					return response;
				}

				// Lấy tất cả lịch hẹn cho dịch vụ này
				var appointments = await _appointmentRepository.FindByPredicate(x =>
					x.ServiceId == serviceId &&
					!x.DeleteStatus);

				if (!appointments.Any())
				{
					response.Success = false;
					response.Message = "Không tìm thấy bác sĩ nào cho dịch vụ này";
					return response;
				}

				// Nhóm theo bác sĩ và tìm bác sĩ có nhiều lịch nhất
				var doctorAppointments = appointments
					.GroupBy(x => x.StaffId)
					.Select(g => new { StaffId = g.Key, Count = g.Count() })
					.OrderByDescending(x => x.Count)
					.FirstOrDefault();

				if (doctorAppointments == null || doctorAppointments.Count == 0)
				{
					response.Success = false;
					response.Message = "Không tìm thấy bác sĩ nào cho dịch vụ này";
					return response;
				}

				// Lấy thông tin chi tiết của bác sĩ
				var staff = await _staffRepository.GetById(doctorAppointments.StaffId.Value);
				if (staff == null || staff.DeleteStatus)
				{
					response.Success = false;
					response.Message = "Thông tin bác sĩ không tồn tại";
					return response;
				}

				response.StaffId = staff.Id;
				response.StaffName = staff.FullName;
				response.Specialization = staff.Specialization;
				response.Degree = staff.Degree;
				response.ExperienceYears = staff.ExperienceYears;
				response.StaffImage = staff.StaffImage;
				response.AppointmentCount = doctorAppointments.Count;
				response.Success = true;
				response.Message = $"Bác sĩ {staff.FullName} có {doctorAppointments.Count} lịch hẹn nhiều nhất cho dịch vụ {service.ServiceName}";

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_BEST_DOCTOR_FOR_SERVICE_ERROR: Exception occurred");
				return new AIBestDoctorResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}"
				};
			}
		}

		/// <summary>Bài 6: Lấy dịch vụ theo khoảng giá</summary>
		public async Task<AIServicesByPriceResponse> GetServicesByPriceRangeAsync(decimal minPrice, decimal maxPrice, int? serviceTypeId = null)
		{
			try
			{
				_logger.LogInformation("GET_SERVICES_BY_PRICE: minPrice={MinPrice}, maxPrice={MaxPrice}, serviceTypeId={ServiceTypeId}", 
					minPrice, maxPrice, serviceTypeId);

				var response = new AIServicesByPriceResponse { Services = new List<AIServicePrice>() };

				var services = await _serviceRepository.FindByPredicate(x =>
					x.Price >= minPrice &&
					x.Price <= maxPrice &&
					!x.DeleteStatus);

				if (serviceTypeId.HasValue)
				{
					services = services.Where(x => x.ServiceTypeId == serviceTypeId).ToList();
				}

				var serviceList = services.Select(s => new AIServicePrice
				{
					ServiceId = s.Id,
					ServiceName = s.ServiceName,
					Price = s.Price ?? 0,
					Description = s.Description,
					ServiceTypeId = s.ServiceTypeId ?? 0
				}).OrderBy(x => x.Price).ToList();

				response.Services = serviceList;
				response.Success = true;
				response.Message = $"Tìm thấy {serviceList.Count} dịch vụ trong khoảng giá {minPrice:C} - {maxPrice:C}";

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_SERVICES_BY_PRICE_ERROR: Exception occurred");
				return new AIServicesByPriceResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}",
					Services = new List<AIServicePrice>()
				};
			}
		}

		/// <summary>Bài 13: Lấy top sản phẩm bán chạy nhất</summary>
		public async Task<AITopProductsResponse> GetTopProductsAsync(int? limit = null, int? serviceTypeId = null)
		{
			try
			{
				_logger.LogInformation("GET_TOP_PRODUCTS: limit={Limit}, serviceTypeId={ServiceTypeId}", limit, serviceTypeId);

				var response = new AITopProductsResponse { Products = new List<AITopProduct>() };

				var products = await _productRepository.FindByPredicate(x => !x.DeleteStatus);

				//if (serviceTypeId.HasValue)
				//{
				//	products = products.Where(x => x.ServiceTypeId == serviceTypeId).ToList();
				//}

				// Đếm số lượng bán cho mỗi sản phẩm
				var productSales = new List<(int ProductId, string ProductName, decimal Price, int SalesCount)>();

				foreach (var product in products)
				{
					var salesCount = (await _invoiceDetailsRepository.FindByPredicate(x =>
						x.ProductId == product.Id &&
						!x.DeleteStatus)).Count();

					productSales.Add((product.Id, product.ProductName, product.SellingPrice ?? 0, salesCount));
				}

				// Sắp xếp và giới hạn số lượng
				var limitCount = limit ?? 3; 
				var topProducts = productSales
					.OrderByDescending(x => x.SalesCount)
					.Take(limitCount)
					.Select(p => new AITopProduct
					{
						ProductId = p.ProductId,
						ProductName = p.ProductName,
						Price = p.Price,
						SalesCount = p.SalesCount
					})
					.ToList();

				response.Products = topProducts;
				response.Success = true;
				response.Message = $"Tìm thấy {topProducts.Count} sản phẩm bán chạy nhất";

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_TOP_PRODUCTS_ERROR: Exception occurred");
				return new AITopProductsResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}",
					Products = new List<AITopProduct>()
				};
			}
		}

		/// <summary>Bài 14: Lấy thông tin chi tiết sản phẩm</summary>
		public async Task<AIProductDetailsResponse> GetProductDetailsAsync(int productId, int? diseaseId = null)
		{
			try
			{
				_logger.LogInformation("GET_PRODUCT_DETAILS: productId={ProductId}, diseaseId={DiseaseId}", productId, diseaseId);

				var response = new AIProductDetailsResponse();

				var product = await _productRepository.GetById(productId);
				if (product == null || product.DeleteStatus)
				{
					response.Success = false;
					response.Message = "Sản phẩm không tồn tại";
					return response;
				}

				// ✅ Đếm số người sử dụng
				var usageCount = (await _invoiceDetailsRepository.FindByPredicate(x =>
					x.ServiceId == productId &&
					!x.DeleteStatus)).Count();

				response.ProductId = product.Id;
				response.ProductName = product.ProductName;
				response.Description = product.Description;
				response.Price = product.SellingPrice ?? 0;
				response.Quantity = product.Quantity;
				response.UserCount = usageCount;

				// 🆕 Thêm thông tin tác dụng sản phẩm (từ DB hoặc lấy trên mạng)
				response.Benefits = await GetProductBenefitsAsync(product);
				response.ImprovementDays = await EstimateImprovementDaysAsync(product);
				response.Success = true;
				response.Message = $"Chi tiết sản phẩm {product.ProductName}";

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_PRODUCT_DETAILS_ERROR: Exception occurred");
				return new AIProductDetailsResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}"
				};
			}
		}

		/// <summary>🆕 Lấy tác dụng sản phẩm từ DB, nếu không có thì gọi LLM để lấy từ mạng</summary>
		private async Task<string> GetProductBenefitsAsync(ProductEntity product)
		{
			try
			{
				// ✅ BƯỚC 1: Kiểm tra Description trong DB
				if (!string.IsNullOrWhiteSpace(product.Description))
				{
					_logger.LogInformation("📌 Using benefits from database for product: {ProductName}", product.ProductName);
					return product.Description;
				}

				// ✅ BƯỚC 2: DB không có → gọi LLM để tìm kiếm từ mạng
				_logger.LogInformation("🔍 Description not found in DB, calling LLM to fetch from internet: {ProductName}", product.ProductName);

				var llmPrompt = $@"Hãy tìm kiếm và mô tả chi tiết tác dụng của sản phẩm: {product.ProductName}

Yêu cầu:
1. Mô tả tác dụng chính của sản phẩm (3-5 điểm)
2. Thành phần hoạt chất chính (nếu có)
3. Đối tượng sử dụng phù hợp
4. Cách sử dụng
5. Lưu ý khi sử dụng

Trả lời bằng tiếng Việt, chi tiết, dễ hiểu và có cấu trúc rõ ràng.";

				var llmResponse = await _llmService.CallLLMAsync(
					"Bạn là một chuyên gia tìm kiếm thông tin sản phẩm trên internet. Hãy cung cấp thông tin chi tiết, chính xác, đáng tin cậy và hữu ích. Tìm kiếm từ các nguồn đáng tin cậy như các website thương mại điện tử, blog sản phẩm, bài viết chuyên ngành.",
					llmPrompt,
					new List<LLMMessage>());

				if (!string.IsNullOrWhiteSpace(llmResponse))
				{
					_logger.LogInformation("✅ Got benefits from LLM for product: {ProductName}", product.ProductName);
					return llmResponse;
				}

				// ✅ BƯỚC 3: Fallback - nếu LLM trả rỗng
				_logger.LogWarning("⚠️ LLM returned empty response for: {ProductName}", product.ProductName);
				return $"Chưa cập nhật thông tin chi tiết về tác dụng của sản phẩm {product.ProductName}. Vui lòng liên hệ: 0383102388";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting product benefits from LLM for product: {ProductName}", product.ProductName);
				return $"Có lỗi khi tìm kiếm thông tin sản phẩm {product.ProductName}. Vui lòng liên hệ: 0383102388";
			}
		}

		/// <summary>🆕 Ước tính thời gian cải thiện (luôn gọi LLM nếu không có trong DB)</summary>
		private async Task<int?> EstimateImprovementDaysAsync(ProductEntity product)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(product.ProductName))
					return null;

				// ✅ BƯỚC 1: Nếu có Description → gọi LLM để ước tính từ description
				if (!string.IsNullOrWhiteSpace(product.Description))
				{
					_logger.LogInformation("📅 Calling LLM to estimate improvement days from DB description: {ProductName}", product.ProductName);

					var llmPrompt = $@"Sản phẩm: {product.ProductName}
Mô tả: {product.Description}

Dựa vào mô tả sản phẩm, hãy ước tính sau bao nhiêu ngày sử dụng sản phẩm này sẽ có hiệu quả rõ rệt.

Trả lời chỉ là một số nguyên (3, 5, 7, 10, 14, 21, 30, ...), không thêm text khác. Ví dụ: 7";

					var llmResponse = await _llmService.CallLLMAsync(
						"Bạn là chuyên gia về sản phẩm chăm sóc da và mỹ phẩm. Hãy ước tính chính xác thời gian cần thiết để sản phẩm có hiệu quả.",
						llmPrompt,
						new List<LLMMessage>());

					if (!string.IsNullOrWhiteSpace(llmResponse) && int.TryParse(llmResponse.Trim(), out var days))
					{
						_logger.LogInformation("✅ Got improvement days from LLM: {Days} days", days);
						return days;
					}
				}

				// ✅ BƯỚC 2: Nếu không có description → gọi LLM để tìm kiếm từ mạng
				_logger.LogInformation("🔍 Calling LLM to find improvement days from internet: {ProductName}", product.ProductName);

				var searchPrompt = $@"Hãy tìm kiếm thông tin về thời gian cần thiết để sản phẩm {product.ProductName} có hiệu quả.

Trả lời chỉ là một số nguyên (3, 5, 7, 10, 14, 21, 30, ...), không thêm text khác. Ví dụ: 7";

				var searchResponse = await _llmService.CallLLMAsync(
					"Bạn là một chuyên gia tìm kiếm thông tin sản phẩm trên internet. Hãy cung cấp thông tin chính xác về thời gian hiệu quả của sản phẩm.",
					searchPrompt,
					new List<LLMMessage>());

				if (!string.IsNullOrWhiteSpace(searchResponse) && int.TryParse(searchResponse.Trim(), out var searchDays))
				{
					_logger.LogInformation("✅ Got improvement days from internet search: {Days} days", searchDays);
					return searchDays;
				}

				_logger.LogWarning("⚠️ Could not estimate improvement days for: {ProductName}", product.ProductName);
				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error estimating improvement days from LLM: {ProductName}", product.ProductName);
				return null;
			}
		}

		/// <summary>Bài 15: Lấy sản phẩm theo khoảng giá</summary>
		public async Task<AIProductsByPriceResponse> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice, int? serviceTypeId = null)
		{
			try
			{
				_logger.LogInformation("GET_PRODUCTS_BY_PRICE: minPrice={MinPrice}, maxPrice={MaxPrice}, serviceTypeId={ServiceTypeId}", 
					minPrice, maxPrice, serviceTypeId);

				var response = new AIProductsByPriceResponse { Products = new List<AIProductPrice>() };

				var products = await _productRepository.FindByPredicate(x =>
					x.SellingPrice >= minPrice &&
					x.SellingPrice <= maxPrice &&
					!x.DeleteStatus);

				//if (serviceTypeId.HasValue)
				//{
				//	products = products.Where(x => x.ServiceTypeId == serviceTypeId).ToList();
				//}

				var productList = products.Select(p => new AIProductPrice
				{
					ProductId = p.Id,
					ProductName = p.ProductName,
					Price = p.SellingPrice ?? 0,
					Description = p.Description,
					Quantity = p.Quantity,
					//ServiceTypeId = p.ServiceTypeId ?? 0
				}).OrderBy(x => x.Price).ToList();

				response.Products = productList;
				response.Success = true;
				response.Message = $"Tìm thấy {productList.Count} sản phẩm trong khoảng giá {minPrice:C} - {maxPrice:C}";

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_PRODUCTS_BY_PRICE_ERROR: Exception occurred");
				return new AIProductsByPriceResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}",
					Products = new List<AIProductPrice>()
				};
			}
		}

		/// <summary>Bài 12: Tư vấn sản phẩm theo yêu cầu/từ khóa (da, mụn, lão hóa, v.v.)</summary>
		public async Task<AIProductsByPriceResponse> GetRecommendedProductsByCategoryAsync(string keyword)
		{
			try
			{
				_logger.LogInformation("GET_RECOMMENDED_PRODUCTS_BY_CATEGORY: keyword={Keyword}", keyword);

				var response = new AIProductsByPriceResponse { Products = new List<AIProductPrice>() };

				if (string.IsNullOrWhiteSpace(keyword))
				{
					response.Success = false;
					response.Message = "Từ khóa không được để trống";
					return response;
				}

				var products = await _productRepository.FindByPredicate(x => !x.DeleteStatus);

				if (!products.Any())
				{
					response.Success = false;
					response.Message = "Không tìm thấy sản phẩm nào";
					return response;
				}

				// Tìm kiếm sản phẩm dựa vào tên, mô tả hoặc từ khóa
				var recommendedProducts = products
					.Where(p =>
						(p.ProductName != null && p.ProductName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
						(p.Description != null && p.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
					.Select(p => new AIProductPrice
					{
						ProductId = p.Id,
						ProductName = p.ProductName,
						Price = p.SellingPrice ?? 0,
						Description = p.Description,
						Quantity = p.Quantity,
						//ServiceTypeId = p.ServiceTypeId ?? 0
					})
					.OrderBy(x => x.ProductName)
					.ToList();

				response.Products = recommendedProducts;
				response.Success = true;
				response.Message = $"Tìm thấy {recommendedProducts.Count} sản phẩm liên quan đến '{keyword}'";

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_RECOMMENDED_PRODUCTS_BY_CATEGORY_ERROR: Exception occurred");
				return new AIProductsByPriceResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}",
					Products = new List<AIProductPrice>()
				};
			}
		}

		/// <summary>Bài 14: Lấy thông tin các gói điều trị của liệu trình trẻ hóa da kèm các buổi điều trị</summary>
		public async Task<AITreatmentPackagesResponse> GetTreatmentPackagesByServiceNameAsync(string serviceName)
		{
			try
			{
				_logger.LogInformation("GET_TREATMENT_PACKAGES: serviceName={ServiceName}", serviceName);

				var response = new AITreatmentPackagesResponse();

				// Lấy dịch vụ theo tên
				var service = await _serviceRepository.FindByPredicate(x =>
					x.ServiceName.Contains(serviceName) &&
					!x.DeleteStatus);

				if (!service.Any())
				{
					response.Success = false;
					response.Message = $"Không tìm thấy dịch vụ '{serviceName}'";
					return response;
				}

				var serviceEntity = service.FirstOrDefault();

				// Ánh xạ thông tin dịch vụ
				response.Service = new ServicePackageInfo
				{
					ServiceId = serviceEntity.Id,
					ServiceName = serviceEntity.ServiceName,
					Description = serviceEntity.Description,
					Price = serviceEntity.Price ?? 0,
					Duration = serviceEntity.Duration,
					ServiceImage = serviceEntity.ServiceImage
				};

				// Lấy tất cả gói điều trị của dịch vụ này
				var treatmentPlans = await _treatmentPlanRepository.FindByPredicate(x =>
					x.ServiceId == serviceEntity.Id &&
					!x.DeleteStatus);

				if (!treatmentPlans.Any())
				{
					response.Success = true;
					response.Message = $"Dịch vụ '{serviceName}' không có gói điều trị nào";
					return response;
				}

				// Xử lý từng gói điều trị
				foreach (var plan in treatmentPlans.OrderBy(x => x.Id))
				{
					var packageInfo = new TreatmentPackageInfo
					{
						PlanId = plan.Id,
						PlanName = plan.PlanName,
						TotalSessions = plan.TotalSessions,
						Price = plan.Price,
						SessionInterval = plan.SessionInterval,
						Description = plan.Description
					};

					// Lấy các buổi điều trị của gói này
					var sessions = await _treatmentSessionRepository.FindByPredicate(x =>
						x.TreatmentPlanId == plan.Id &&
						!x.DeleteStatus);

					// Ánh xạ thông tin buổi điều trị
					packageInfo.Sessions = sessions
						.OrderBy(x => x.SessionNumber ?? 0)
						.Select(s => new TreatmentSessionInfo
						{
							SessionId = s.Id,
							SessionNumber = s.SessionNumber,
							SessionName = s.SessionName,
							Description = s.Description,
							Duration = s.Duration
						})
						.ToList();

					response.TreatmentPackages.Add(packageInfo);
				}

				response.Success = true;
				response.Message = $"Lấy thông tin {response.TreatmentPackages.Count} gói điều trị của dịch vụ '{serviceName}' thành công";

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_TREATMENT_PACKAGES_ERROR: Exception occurred");
				return new AITreatmentPackagesResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}"
				};
			}
		}
	}
}