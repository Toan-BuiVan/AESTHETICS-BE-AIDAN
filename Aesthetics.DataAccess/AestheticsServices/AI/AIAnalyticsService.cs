using Aesthetics.Data.AestheticsInterfaces.AI;
using Aesthetics.Data.RepositoryInterfaces;
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

		public AIAnalyticsService(
			ILogger<AIAnalyticsService> logger,
			IServiceRepository serviceRepository,
			IAppointmentRepositoty appointmentRepository,
			IProductRepository productRepository,
			ITreatmentPlanRepository treatmentPlanRepository,
			IInvoiceDetailsRepository invoiceDetailsRepository,
			ICartProductRepository cartProductRepository,
			IStaffRepository staffRepository)
		{
			_logger = logger;
			_serviceRepository = serviceRepository;
			_appointmentRepository = appointmentRepository;
			_productRepository = productRepository;
			_treatmentPlanRepository = treatmentPlanRepository;
			_invoiceDetailsRepository = invoiceDetailsRepository;
			_cartProductRepository = cartProductRepository;
			_staffRepository = staffRepository;
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
		public async Task<AITopProductsResponse> GetTopProductsAsync(int? serviceTypeId = null)
		{
			try
			{
				_logger.LogInformation("GET_TOP_PRODUCTS: serviceTypeId={ServiceTypeId}", serviceTypeId);

				var response = new AITopProductsResponse { Products = new List<AITopProduct>() };

				var products = await _productRepository.FindByPredicate(x => !x.DeleteStatus);

				if (serviceTypeId.HasValue)
				{
					products = products.Where(x => x.ServiceTypeId == serviceTypeId).ToList();
				}

				// Đếm số lượng bán cho mỗi sản phẩm
				var productSales = new List<(int ProductId, string ProductName, decimal Price, int SalesCount)>();

				foreach (var product in products)
				{
					var salesCount = (await _invoiceDetailsRepository.FindByPredicate(x =>
						x.ServiceId == product.Id &&
						!x.DeleteStatus)).Count();

					productSales.Add((product.Id, product.ProductName, product.SellingPrice ?? 0, salesCount));
				}

				var topProducts = productSales
					.OrderByDescending(x => x.SalesCount)
					.Take(10)
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

				// Đếm số người sử dụng
				var usageCount = (await _invoiceDetailsRepository.FindByPredicate(x =>
					x.ServiceId == productId &&
					!x.DeleteStatus)).Count();

				response.ProductId = product.Id;
				response.ProductName = product.ProductName;
				response.Description = product.Description;
				response.Price = product.SellingPrice ?? 0;
				response.Quantity = product.Quantity;
				response.UserCount = usageCount;
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

				if (serviceTypeId.HasValue)
				{
					products = products.Where(x => x.ServiceTypeId == serviceTypeId).ToList();
				}

				var productList = products.Select(p => new AIProductPrice
				{
					ProductId = p.Id,
					ProductName = p.ProductName,
					Price = p.SellingPrice ?? 0,
					Description = p.Description,
					Quantity = p.Quantity,
					ServiceTypeId = p.ServiceTypeId ?? 0
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
	}
}