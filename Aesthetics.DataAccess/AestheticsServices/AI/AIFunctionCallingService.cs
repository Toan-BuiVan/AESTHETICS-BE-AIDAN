using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.AI;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Enum;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.RequestModel.AI;
using Aesthetics.Entities.Models.ResponseModel.AI;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices.AI
{
	public class AIFunctionCallingService : IAIFunctionCallingService
	{
		private readonly ILogger<AIFunctionCallingService> _logger;
		private readonly IAIAppointmentService _aiAppointmentService;
		private readonly IAIAnalyticsService _aiAnalyticsService;	
		private readonly IAICartService _aiCartService;
		private readonly ILLMService _llmService;
		private readonly IStaffRepository _staffRepository;
		private readonly IServiceRepository _serviceRepository;
		private readonly ITreatmentPlanRepository _treatmentPlanRepository;
		private readonly ITreatmentSessionRepository _treatmentSessionRepository;
		private readonly IAppointmentRepositoty _appointmentRepository;
		private readonly IProductRepository _productRepository;
		private readonly ICustomerTreatmentPlansRepository _customerTreatmentPlansRepository;
		private readonly ICustomerTreatmentSessionsRepository _customerTreatmentSessionsRepository;
		private readonly IAppointmentAssignmentRepository _appointmentAssignmentRepository;

		public AIFunctionCallingService(
			ILogger<AIFunctionCallingService> logger,
			IAIAppointmentService aiAppointmentService,
			IAIAnalyticsService aiAnalyticsService,
			IAICartService aiCartService,
			ILLMService llmService,
			IStaffRepository staffRepository,
			IServiceRepository serviceRepository,
			ITreatmentPlanRepository treatmentPlanRepository,
			ITreatmentSessionRepository treatmentSessionRepository,
			IAppointmentRepositoty appointmentRepository,
			ICustomerTreatmentPlansRepository customerTreatmentPlansRepository,
			ICustomerTreatmentSessionsRepository customerTreatmentSessionsRepository,
			IAppointmentAssignmentRepository appointmentAssignmentRepository,
			IProductRepository productRepository)  
		{
			_logger = logger;
			_aiAppointmentService = aiAppointmentService;
			_aiAnalyticsService = aiAnalyticsService;
			_aiCartService = aiCartService;
			_llmService = llmService;
			_staffRepository = staffRepository;
			_productRepository = productRepository;
			_serviceRepository = serviceRepository;
			_treatmentPlanRepository = treatmentPlanRepository;  
			_treatmentSessionRepository = treatmentSessionRepository;
			_appointmentRepository = appointmentRepository;
			_customerTreatmentPlansRepository = customerTreatmentPlansRepository;
			_customerTreatmentSessionsRepository = customerTreatmentSessionsRepository;
			_appointmentAssignmentRepository = appointmentAssignmentRepository;
		}

		public async Task<AIToolsListResponse> GetAvailableToolsAsync()
		{
			try
			{
				_logger.LogInformation("Getting available tools for AI Function Calling");

				var tools = new List<AITool>
				{
					// ===== APPOINTMENT TOOLS =====
					new AITool
					{
						Name = "getDoctorAvailableSlots",
						Description = "Lấy danh sách slot trống của bác sĩ trong một ngày. Loại bỏ: giờ có lịch, giờ nghỉ trưa 12-13h, giờ khóa trong AppointmentTimeLocks",
						InputSchema = new Dictionary<string, string>
						{
							{ "staffId", "int - ID bác sĩ" },
							{ "date", "string (YYYY-MM-DD) - Ngày cần kiểm tra" }
						},
						OutputDescription = "Danh sách slot trống: [{ date, time, staffId, staffName }]",
						Example = "staffId: 0, date: 2026-04-10"
					},

					new AITool
					{
						Name = "getAvailableSlotsForService",
						Description = "Tìm bác sĩ của một dịch vụ và trả về slot trống của họ trong ngày. Kết hợp 2 bước: tìm bác sĩ + lấy lịch trống",
						InputSchema = new Dictionary<string, string>
						{
							{ "serviceId", "int - ID dịch vụ" },
							{ "date", "string (YYYY-MM-DD) - Ngày cần kiểm tra" },
							{ "treatmentPlanId", "int (optional) - ID liệu trình nếu có" }
						},
						OutputDescription = "Danh sách bác sĩ có slot trống: [{ staffId, name, specialization, AvailableSlots: [{ date, time }] }]",
						Example = "serviceId: 1, date: 2026-04-08"
					},

					new AITool
					{
						Name = "getTreatmentPackagesByServiceName",
						Description = "⭐ Lấy thông tin các gói điều trị của liệu trình (ví dụ: trẻ hóa da) kèm các buổi điều trị chi tiết",
						InputSchema = new Dictionary<string, string>
						{
							{ "serviceName", "string - Tên dịch vụ/liệu trình (ví dụ: 'trẻ hóa da', 'trị nám', 'chăm sóc da')" }
						},
						OutputDescription = "Thông tin dịch vụ và danh sách gói: { service: { name, price, description }, packages: [{ planName, totalSessions, price, description, sessions: [{ sessionNumber, name, description, duration }] }] }",
						Example = "serviceName: 'trẻ hóa da' hoặc 'trị nám' hoặc 'chăm sóc da'"
					},

					new AITool
					{
						Name = "getDoctorAvailableSlotsForTreatmentPlan",
						Description = "Lấy slot trống của bác sĩ cho liệu trình cụ thể trong một ngày",
						InputSchema = new Dictionary<string, string>
						{
							{ "staffId", "int - ID bác sĩ" },
							{ "treatmentPlanId", "int - ID liệu trình" },
							{ "date", "string (YYYY-MM-DD) - Ngày cần kiểm tra" }
						},
						OutputDescription = "Danh sách slot trống: [{ date, time, staffId, staffName }]",
						Example = "staffId: 0, treatmentPlanId: 0, date: 2000-00-00"
					},

					new AITool
					{
						Name = "getDoctorsForService",
						Description = "Lấy danh sách bác sĩ của một dịch vụ",
						InputSchema = new Dictionary<string, string>
						{
							{ "serviceId", "int - ID dịch vụ" }
						},
						OutputDescription = "Danh sách bác sĩ: [{ staffId, name, specialization, experience, degree, rating }]",
						Example = "serviceId: 0"
					},

					new AITool
					{
						Name = "bookAppointment",
						Description = "Đặt lịch khám cho khách hàng",
						InputSchema = new Dictionary<string, string>
						{
							{ "customerId", "int - ID khách hàng" },
							{ "staffId", "int - ID bác sĩ" },
							{ "serviceId", "int - ID dịch vụ" },
							{ "appointmentDate", "string (YYYY-MM-DD) - Ngày hẹn" },
							{ "appointmentTime", "string (HH:mm) - Giờ hẹn" }
						},
						OutputDescription = "Thông tin lịch hẹn: { appointmentId, confirmationCode, status }",
						Example = "customerId: 0, staffId: 0, serviceId: 0, appointmentDate: 2000-00-00, appointmentTime: 09:00"
					},

					new AITool
					{
						Name = "cancelAppointment",
						Description = "Hủy lịch hẹn của khách hàng với bác sĩ (có thể lọc theo ngày, dịch vụ)",
						InputSchema = new Dictionary<string, string>
						{
							{ "customerId", "int - ID khách hàng" },
							{ "staffId", "int - ID bác sĩ" },
							{ "appointmentDate", "string? (YYYY-MM-DD) - Ngày hẹn (optional)" },
							{ "serviceId", "int? - ID dịch vụ (optional)" }
						},
						OutputDescription = "{ cancelledCount }",
						Example = "customerId: 0, staffId: 0, appointmentDate: 2000-00-00"
					},

					new AITool
					{
						Name = "getDoctorsForTreatmentPlan",
						Description = "⭐ Lấy DANH SÁCH TẤT CẢ bác sĩ của một liệu trình (bất kể có lịch hẹn hay không)",
						InputSchema = new Dictionary<string, string>
						{
							{ "treatmentPlanId", "int - ID liệu trình" }
						},
						OutputDescription = "Danh sách tất cả bác sĩ: [{ staffId, name, specialization, experience, degree, rating, appointmentCount }]",
						Example = "treatmentPlanId: 12"
					},

					new AITool
					{
						Name = "bookAppointment",
						Description = "Đặt lịch khám cho khách hàng (hỗ trợ đặt lịch cho buổi cụ thể trong liệu trình)",
						InputSchema = new Dictionary<string, string>
						{
							{ "customerId", "int - ID khách hàng" },
							{ "staffId", "int - ID bác sĩ" },
							{ "serviceId", "int - ID dịch vụ" },
							{ "appointmentDate", "string (YYYY-MM-DD) - Ngày hẹn" },
							{ "appointmentTime", "string (HH:mm) - Giờ hẹn" },
							{ "treatmentPlanId", "int (optional) - ID liệu trình" },
							{ "sessionNumber", "int (optional) - Buổi thứ mấy trong liệu trình (ví dụ: 1, 2, 3...)" }
						},
						OutputDescription = "Thông tin lịch hẹn: { appointmentId, confirmationCode, status }",
						Example = "customerId: 1, staffId: 1, serviceId: 1, appointmentDate: 2026-04-08, appointmentTime: 09:00, treatmentPlanId: 1, sessionNumber: 3"
					},

					// ===== ANALYTICS TOOLS =====
					new AITool
					{
						Name = "getMostPopularServices",
						Description = "Dịch vụ được đặt lịch nhiều nhất",
						InputSchema = new Dictionary<string, string>(),
						OutputDescription = "Danh sách dịch vụ: [{ serviceId, name, price, appointmentCount }]",
						Example = ""
					},

					new AITool
					{
						Name = "getBestDoctorForService",
						Description = "Bác sĩ có nhiều lịch đặt nhất cho dịch vụ",
						InputSchema = new Dictionary<string, string>
						{
							{ "serviceId", "int - ID dịch vụ" }
						},
						OutputDescription = "Thông tin bác sĩ: { staffId, name, specialization, appointmentCount, rating }",
						Example = "serviceId: 0"
					},

					new AITool
					{
						 Name = "getServicesByPriceRange",
						 Description = "Dịch vụ/liệu trình theo khoảng giá",
						 InputSchema = new Dictionary<string, string>
						 {
							 { "minPrice", "decimal - Giá tối thiểu" },
							 { "maxPrice", "decimal - Giá tối đa" }
						 },
						 OutputDescription = "{ services: [...] }",
						 Example = "minPrice: 100000, maxPrice: 500000"
					},

					new AITool
					{
						Name = "getRecommendedProductsByCategory",
						Description = "⭐ Tư vấn sản phẩm theo yêu cầu/loại (da, mụn, lão hóa, v.v.) - tìm sản phẩm liên quan đến yêu cầu",
						InputSchema = new Dictionary<string, string>
						{
							{ "keyword", "string - Từ khóa tìm kiếm (ví dụ: 'da', 'mụn', 'lão hóa', 'chăm sóc da', 'trị nám')" }
						},
						OutputDescription = "Danh sách sản phẩm liên quan: [{ productId, name, price, description }]",
						Example = "keyword: 'chăm sóc da' hoặc 'mụn' hoặc 'da khô'"
					},

					new AITool
					{
						Name = "getTopSellingProducts",
						Description = "Top sản phẩm bán chạy nhất (có thể giới hạn số lượng)",
						InputSchema = new Dictionary<string, string>
						{
							{ "limit", "int (optional) - Số lượng sản phẩm muốn lấy (default: 10)" }
						},
						OutputDescription = "Danh sách sản phẩm: [{ productId, name, price, salesCount }]",
						Example = "limit: 5 hoặc không truyền để lấy top 10"
					},

					new AITool
					{
						Name = "getProductDetail",
						Description = "Chi tiết sản phẩm và tác dụng",
						InputSchema = new Dictionary<string, string>
						{
							{ "productId", "int - ID sản phẩm" }
						},
						OutputDescription = "{ productId, name, description, price, quantity, userCount }",
						Example = "productId: 5"
					},

					new AITool
					{
						 Name = "getProductsByPriceRange",
						 Description = "Sản phẩm theo khoảng giá",
						 InputSchema = new Dictionary<string, string>
						 {
							 { "minPrice", "decimal - Giá tối thiểu" },
							 { "maxPrice", "decimal - Giá tối đa" }
						 },
						 OutputDescription = "Danh sách sản phẩm: [{ productId, name, price, description }]",
						 Example = "minPrice: 100000, maxPrice: 500000"
					},

					// ===== CART TOOLS =====
					new AITool
					{
						Name = "addProductToCart",
						Description = "Thêm sản phẩm vào giỏ hàng",
						InputSchema = new Dictionary<string, string>
						{
							{ "productId", "int - ID sản phẩm" },
							{ "quantity", "int (default: 1) - Số lượng" }
						},
						OutputDescription = "{ cartProductId, productId, quantity, price }",
						Example = "productId: 5, quantity: 2"
					},

					new AITool
					{
						Name = "removeProductFromCart",
						Description = "Xóa sản phẩm khỏi giỏ hàng",
						InputSchema = new Dictionary<string, string>
						{
							{ "productId", "int - ID sản phẩm" }
						},
						OutputDescription = "{ success, message }",
						Example = "productId: 5"
					}
				};

				return new AIToolsListResponse
				{
					Tools = tools,
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting available tools");
				throw;
			}
		}

		public async Task<AIExecuteToolResponse> ExecuteToolAsync(AIExecuteToolRequest request)
		{
			try
			{
				if (request?.LLMResponse == null)
				{
					return new AIExecuteToolResponse
					{
						Success = false,
						Error = "Invalid LLM response"
					};
				}

				_logger.LogInformation("Executing tool: {Tool} with params: {@Params}",
					request.LLMResponse.Tool, request.LLMResponse.Params);

				var isValid = await ValidateToolParamsAsync(request.LLMResponse.Tool, request.LLMResponse.Params);
				if (!isValid)
				{
					return new AIExecuteToolResponse
					{
						Success = false,
						Error = "Invalid parameters for tool"
					};
				}

				var result = request.LLMResponse.Tool switch
				{
					// Appointment tools
					"getDoctorAvailableSlots" => await ExecuteGetDoctorAvailableSlots(request.LLMResponse.Params),
					"getAvailableSlotsForService" => await ExecuteGetAvailableSlotsForService(request.LLMResponse.Params),
					"getDoctorAvailableSlotsForTreatmentPlan" => await ExecuteGetDoctorAvailableSlotsForTreatmentPlan(request.LLMResponse.Params),
					"getDoctorsForService" => await ExecuteGetDoctorsForService(request.LLMResponse.Params),
					"bookAppointment" => await ExecuteBookAppointment(request.LLMResponse.Params, request.UserId),
					"cancelAppointment" => await ExecuteCancelAppointment(request.LLMResponse.Params, request.UserId),
					"getDoctorsForTreatmentPlan" => await ExecuteGetDoctorsForTreatmentPlan(request.LLMResponse.Params),

					// Analytics tools
					"getMostPopularServices" => await ExecuteGetMostPopularServices(request.LLMResponse.Params),
					"getBestDoctorForService" => await ExecuteGetBestDoctorForService(request.LLMResponse.Params),
					"getServicesByPriceRange" => await ExecuteGetServicesByPriceRange(request.LLMResponse.Params),
					"getRecommendedProductsByCategory" => await ExecuteGetRecommendedProductsByCategory(request.LLMResponse.Params),
					"getTopSellingProducts" => await ExecuteGetTopSellingProducts(request.LLMResponse.Params),
					"getProductDetail" => await ExecuteGetProductDetail(request.LLMResponse.Params),
					"getProductsByPriceRange" => await ExecuteGetProductsByPriceRange(request.LLMResponse.Params),
					"getTreatmentPackagesByServiceName" => await ExecuteGetTreatmentPackagesByServiceName(request.LLMResponse.Params),

					// Cart tools
					"addProductToCart" => await ExecuteAddProductToCart(request.LLMResponse.Params, request.UserId),
					"removeProductFromCart" => await ExecuteRemoveProductFromCart(request.LLMResponse.Params, request.UserId),

					_ => new AIExecuteToolResponse { Success = false, Error = "Tool not found" }
				};

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error executing tool: {Tool}", request?.LLMResponse?.Tool);
				return new AIExecuteToolResponse
				{
					Success = false,
					Error = ex.Message
				};
			}
		}

		public async Task<bool> ValidateToolParamsAsync(string toolName, Dictionary<string, object> @params)
		{
			try
			{
				return toolName switch
				{
					"getDoctorAvailableSlots" => ValidateDoctorSlotsParams(@params),
					"getDoctorAvailableSlotsForTreatmentPlan" => ValidateDoctorTreatmentSlotsParams(@params),
					"getDoctorsForService" => ValidateServiceParams(@params),
					"getDoctorsForTreatmentPlan" => ValidateTreatmentPlanParams(@params),  
					"bookAppointment" => ValidateBookingParams(@params),
					"cancelAppointment" => ValidateCancelParams(@params),
					"getMostPopularServices" => true,
					"getBestDoctorForService" => ValidateServiceParams(@params),
					"getServicesByPriceRange" => ValidatePriceRangeParams(@params),
					"getTopSellingProducts" => true,
					"getRecommendedProductsByCategory" => ValidateKeywordParams(@params),
					"getProductDetail" => ValidateProductParams(@params),
					"getProductsByPriceRange" => ValidatePriceRangeParams(@params),
					"getTreatmentPackagesByServiceName" => ValidateServiceNameParams(@params),
					"addProductToCart" => ValidateAddToCartParams(@params),
					"removeProductFromCart" => ValidateProductParams(@params),
					"getAvailableSlotsForService" => ValidateAvailableSlotsForServiceParams(@params),
					_ => false
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Validation error for tool {Tool}", toolName);
				return false;
			}
		}

		/// <summary>
		/// Chuẩn hóa định dạng giờ từ nhiều format khác nhau
		/// Hỗ trợ: "08:30", "8:30", "0830", "14", "14h", "2h chiều", "8h30 sáng", v.v.
		/// </summary>
		private string NormalizeTime(string timeStr)
		{
			if (string.IsNullOrWhiteSpace(timeStr))
				return null;

			timeStr = timeStr.Trim().ToLower();
			_logger.LogInformation("📝 Normalizing time: {RawTime}", timeStr);

			try
			{
				// Case 1: Đã đúng format HH:mm
				if (timeStr.Contains(":"))
				{
					if (DateTime.TryParse($"2000-01-01 {timeStr}", out var result))
					{
						return result.ToString("HH:mm");
					}
				}

				// Case 2: Format số không có dấu (0830, 830, 14, 8, v.v.)
				if (int.TryParse(timeStr, out var timeInt))
				{
					int hour = timeInt / 100;
					int minute = timeInt % 100;

					if (hour >= 0 && hour < 24 && minute >= 0 && minute < 60)
					{
						return $"{hour:D2}:{minute:D2}";
					}
				}

				// Case 3: Format "8h30 sáng", "14h chiều", "3h sáng", v.v.
				// Xử lý từ khóa "sáng", "trưa", "chiều", "tối"
				string timeText = timeStr
					.Replace("h", ":")
					.Replace("sáng", "") // 8:30 sáng → 8:30 (không cần xử lý)
					.Replace("trưa", "") // 12:00 trưa → 12:00
					.Replace("chiều", "+12") // 3h chiều → 3+12:00 = 15:00
					.Replace("tối", "+12") // 8h tối → 8+12:00 = 20:00
					.Trim();

				// Parse "3+12:00" → 15:00
				if (timeText.Contains("+"))
				{
					var parts = timeText.Split('+');
					if (int.TryParse(parts[0], out var baseHour) && int.TryParse(parts[1], out var offset))
					{
						int finalHour = baseHour + offset;
						if (finalHour >= 0 && finalHour < 24)
						{
							return $"{finalHour:D2}:00";
						}
					}
				}

				// Thử parse như time bình thường
				if (DateTime.TryParse($"2000-01-01 {timeText}", out var parsedTime))
				{
					return parsedTime.ToString("HH:mm");
				}

				_logger.LogWarning("❌ Could not normalize time: {TimeStr}", timeStr);
				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error normalizing time: {TimeStr}", timeStr);
				return null;
			}
		}

		// ===== EXECUTION METHODS =====
		private async Task<AIExecuteToolResponse> ExecuteGetDoctorAvailableSlots(Dictionary<string, object> @params)
		{
			try
			{
				var staffId = Convert.ToInt32(@params["staffId"]);
				var dateStr = @params["date"].ToString();
				if (!DateTime.TryParse(dateStr, out var date))
				{
					return new AIExecuteToolResponse { Success = false, Error = "Invalid date format" };
				}

				var result = await _aiAppointmentService.GetDoctorAvailableSlotsAsync(staffId, date);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteGetDoctorAvailableSlots");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteGetAvailableSlotsForService(Dictionary<string, object> @params)
		{
			try
			{
				var serviceId = Convert.ToInt32(@params["serviceId"]);
				var dateStr = @params["date"].ToString();
				if (!DateTime.TryParse(dateStr, out var date))
				{
					return new AIExecuteToolResponse { Success = false, Error = "Invalid date format" };
				}

				int? treatmentPlanId = null;
				if (@params.ContainsKey("treatmentPlanId") && @params["treatmentPlanId"] != null)
				{
					if (int.TryParse(@params["treatmentPlanId"].ToString(), out var tpId))
					{
						treatmentPlanId = tpId;
					}
				}

				var result = await _aiAppointmentService.GetAvailableSlotsForServiceAsync(serviceId, date, treatmentPlanId);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message	
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteGetAvailableSlotsForService");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteGetDoctorAvailableSlotsForTreatmentPlan(Dictionary<string, object> @params)
		{
			try
			{
				var staffId = Convert.ToInt32(@params["staffId"]);
				var treatmentPlanId = Convert.ToInt32(@params["treatmentPlanId"]);
				var dateStr = @params["date"].ToString();
				if (!DateTime.TryParse(dateStr, out var date))
				{
					return new AIExecuteToolResponse { Success = false, Error = "Invalid date format" };
				}

				var result = await _aiAppointmentService.GetDoctorAvailableSlotsForTreatmentPlanAsync(staffId, treatmentPlanId, date);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteGetDoctorAvailableSlotsForTreatmentPlan");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteGetDoctorsForService(Dictionary<string, object> @params)
		{
			try
			{
				var serviceId = Convert.ToInt32(@params["serviceId"]);
				var result = await _aiAppointmentService.GetDoctorsForServiceAsync(serviceId);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteGetDoctorsForService");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteBookAppointment(Dictionary<string, object> @params, int userId)
		{
			try
			{
				//var customerId = Convert.ToInt32(@params["customerId"]);
				var customerId = userId;
				var staffId = Convert.ToInt32(@params["staffId"]);
				var serviceId = Convert.ToInt32(@params["serviceId"]);
				var appointmentDateStr = @params["appointmentDate"].ToString();
				var appointmentTimeStr = @params["appointmentTime"].ToString();

				// ✅ Parse date
				if (!DateTime.TryParse(appointmentDateStr, out var appointmentDate))
				{
					return new AIExecuteToolResponse { Success = false, Error = "Invalid appointment date format" };
				}

				// ✅ Parse time - support multiple formats
				var appointmentTime = NormalizeTime(appointmentTimeStr);
				if (string.IsNullOrEmpty(appointmentTime))
				{
					return new AIExecuteToolResponse { Success = false, Error = "Invalid appointment time format. Use HH:mm (e.g., 08:30, 14:00)" };
				}

				_logger.LogInformation("🕐 Normalized time: {RawTime} → {NormalizedTime}", appointmentTimeStr, appointmentTime);

				int? treatmentPlanId = null;
				if (@params.ContainsKey("treatmentPlanId") && @params["treatmentPlanId"] != null)
				{
					if (int.TryParse(@params["treatmentPlanId"].ToString(), out var tpId))
					{
						treatmentPlanId = tpId;
					}
				}

				int? sessionNumber = null;
				if (@params.ContainsKey("sessionNumber") && @params["sessionNumber"] != null)
				{
					if (int.TryParse(@params["sessionNumber"].ToString(), out var sn))
					{
						sessionNumber = sn;
					}
				}

				var result = await _aiAppointmentService.BookAppointmentAsync(
					customerId, staffId, serviceId, appointmentDate, appointmentTime, treatmentPlanId, sessionNumber);

				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteBookAppointment");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteCancelAppointment(Dictionary<string, object> @params, int userId)
		{
			try
			{
				_logger.LogInformation("=== EXECUTE CANCEL APPOINTMENT START ===");
				_logger.LogInformation("Params: {@Params}", @params);

				// ✅ STEP 1: Validate customerId
				if (!@params.ContainsKey("customerId") || @params["customerId"] == null)
				{
					return CreateErrorResponse("customerId không được để trống");
				}

				if (!int.TryParse(@params["customerId"].ToString(), out var customerId))
				{
					return CreateErrorResponse("customerId phải là số");
				}

				// ✅ STEP 2: Validate & resolve staffId (có thể là tên bác sĩ hoặc ID)
				if (!@params.ContainsKey("staffId") || @params["staffId"] == null)
				{
					return CreateErrorResponse("staffId không được để trống");
				}

				var staffIdValue = @params["staffId"].ToString();
				int staffId;

				if (int.TryParse(staffIdValue, out var parsedStaffId))
				{
					staffId = parsedStaffId;
					_logger.LogInformation("✓ staffId is numeric: {StaffId}", staffId);
				}
				else
				{
					_logger.LogInformation("🔍 staffId is a name, looking up: {StaffName}", staffIdValue);
					var foundStaffId = await FindStaffIdByNameAsync(staffIdValue);

					if (!foundStaffId.HasValue)
					{
						_logger.LogError("❌ Could not find staff with name: {StaffName}", staffIdValue);
						return CreateErrorResponse($"Không tìm thấy bác sĩ: {staffIdValue}");
					}

					staffId = foundStaffId.Value;
					_logger.LogInformation("✓ Found staff: '{Name}' → ID: {StaffId}", staffIdValue, staffId);
				}

				// ✅ STEP 3: Parse appointmentDate (optional)
				DateTime? appointmentDate = null;
				if (@params.ContainsKey("appointmentDate") && @params["appointmentDate"] != null)
				{
					var dateStr = @params["appointmentDate"].ToString();
					if (!DateTime.TryParse(dateStr, out var parsedDate))
					{
						return CreateErrorResponse($"Định dạng ngày không hợp lệ: {dateStr}. Vui lòng dùng YYYY-MM-DD");
					}
					appointmentDate = parsedDate;
					_logger.LogInformation("✓ appointmentDate parsed: {Date}", appointmentDate?.ToString("dd-MM-yyyy"));
				}

				// ✅ STEP 4: Parse serviceId/serviceName (optional)
				int? serviceId = null;
				if (@params.ContainsKey("serviceId") && @params["serviceId"] != null)
				{
					var serviceIdValue = @params["serviceId"].ToString();

					if (int.TryParse(serviceIdValue, out var parsedServiceId))
					{
						serviceId = parsedServiceId;
						_logger.LogInformation("✓ serviceId is numeric: {ServiceId}", serviceId);
					}
					else
					{
						_logger.LogInformation("🔍 serviceId is a name, looking up: {ServiceName}", serviceIdValue);
						var foundServiceId = await FindServiceIdByNameAsync(serviceIdValue);

						if (!foundServiceId.HasValue)
						{
							_logger.LogWarning("⚠ Could not find service with name: {ServiceName}", serviceIdValue);
						}
						else
						{
							serviceId = foundServiceId.Value;
							_logger.LogInformation("✓ Found service: '{Name}' → ID: {ServiceId}", serviceIdValue, serviceId);
						}
					}
				}

				// ✅ STEP 5: Parse sessionNumber (optional - số buổi cụ thể)
				int? sessionNumber = null;
				if (@params.ContainsKey("sessionNumber") && @params["sessionNumber"] != null)
				{
					if (int.TryParse(@params["sessionNumber"].ToString(), out var parsedSessionNumber))
					{
						sessionNumber = parsedSessionNumber;
						_logger.LogInformation("🔍 sessionNumber specified: Buổi thứ {SessionNumber}", sessionNumber);
					}
				}

				// ✅ STEP 6: Phân tích case và gọi hàm xử lý phù hợp
				_logger.LogInformation(
					"[CASE ANALYSIS] CustomerId: {CustomerId}, StaffId: {StaffId}, Date: {Date}, ServiceId: {ServiceId}, SessionNumber: {SessionNumber}",
					customerId, staffId, appointmentDate?.ToString("yyyy-MM-dd"), serviceId, sessionNumber);

				AIExecuteToolResponse result;

				// ✅ **CASE SPECIAL**: Hủy lịch hẹn buổi 2 gói trị liệu trẻ hóa da với bác sĩ Ha ngày 25-04-2026
				if (sessionNumber.HasValue && serviceId.HasValue)
				{
					_logger.LogInformation("📍 CASE SPECIAL: Hủy buổi {SessionNumber} của dịch vụ {ServiceId} với bác sĩ {StaffId}",sessionNumber, serviceId, staffId);
					result = await HandleCancelSpecificSessionAppointment(customerId, staffId, serviceId.Value, sessionNumber.Value,appointmentDate);
				}
				// Case 2: Hủy lịch hẹn với bác sĩ trong NGÀY CỤ THỂ: Cho tôi hủy lịch hẹn với bác sĩ Ha ngày 29-04-2026
				else if (appointmentDate.HasValue && serviceId == null)
				{
					_logger.LogInformation("📍 CASE 2: Hủy lịch hẹn với bác sĩ {StaffId} vào ngày {Date}", staffId, appointmentDate?.ToString("yyyy-MM-dd"));
					result = await HandleCancelAppointmentsByDate(customerId, staffId, appointmentDate.Value);
				}
				// Case 1: Hủy TẤT CẢ lịch hẹn với bác sĩ (không có ngày, không có dịch vụ)
				else if (appointmentDate == null && serviceId == null)
				{
					_logger.LogInformation("📍 CASE 1: Hủy TẤT CẢ lịch hẹn với bác sĩ {StaffId}", staffId);
					result = await HandleCancelAllAppointmentsWithDoctor(customerId, staffId);
				}
				// Case 3: Hủy lịch hẹn với bác sĩ cho DỊCH VỤ CỤ THỂ (có thể là dịch vụ đơn lẻ hoặc liệu trình)
				else if (serviceId.HasValue && appointmentDate == null)
				{
					_logger.LogInformation("📍 CASE 3: Hủy lịch hẹn với bác sĩ {StaffId} cho dịch vụ {ServiceId}", staffId, serviceId);
					result = await HandleCancelAppointmentsByService(customerId, staffId, serviceId.Value);
				}
				// Case 4: Hủy lịch hẹn với bác sĩ trong NGÀY CỤ THỂ cho DỊCH VỤ CỤ THỂ
				else if (appointmentDate.HasValue && serviceId.HasValue)
				{
					_logger.LogInformation("📍 CASE 4: Hủy lịch hẹn với bác sĩ {StaffId} vào ngày {Date} cho dịch vụ {ServiceId}",
						staffId, appointmentDate?.ToString("yyyy-MM-dd"), serviceId);
					result = await HandleCancelAppointmentsByDateAndService(customerId, staffId, appointmentDate.Value, serviceId.Value);
				}
				else
				{
					result = CreateErrorResponse("Không thể xác định case hủy lịch hẹn");
				}

				_logger.LogInformation("=== EXECUTE CANCEL APPOINTMENT END - Success: {Success} ===", result.Success);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ Error in ExecuteCancelAppointment");
				return CreateErrorResponse($"Lỗi: {ex.Message}");
			}
		}
		
		private async Task<AIExecuteToolResponse> ExecuteGetMostPopularServices(Dictionary<string, object> @params)
		{
			try
			{
				var result = await _aiAnalyticsService.GetMostUsedServiceAsync();
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteGetMostPopularServices");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteGetBestDoctorForService(Dictionary<string, object> @params)
		{
			try
			{
				var serviceId = Convert.ToInt32(@params["serviceId"]);
				var result = await _aiAnalyticsService.GetBestDoctorForServiceAsync(serviceId);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteGetBestDoctorForService");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteGetServicesByPriceRange(Dictionary<string, object> @params)
		{
			try
			{
				var minPrice = Convert.ToDecimal(@params["minPrice"]);
				var maxPrice = Convert.ToDecimal(@params["maxPrice"]);
				var result = await _aiAnalyticsService.GetServicesByPriceRangeAsync(minPrice, maxPrice);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteGetServicesByPriceRange");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteGetTopSellingProducts(Dictionary<string, object> @params)
		{
			try
			{
				int? limit = null;
				if (@params.ContainsKey("limit") && @params["limit"] != null)
				{
					if (int.TryParse(@params["limit"].ToString(), out var limitValue))
					{
						limit = limitValue;
					}
				}

				var result = await _aiAnalyticsService.GetTopProductsAsync(limit);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteGetTopSellingProducts");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteGetProductDetail(Dictionary<string, object> @params)
		{
			try
			{
				var productId = Convert.ToInt32(@params["productId"]);
				var result = await _aiAnalyticsService.GetProductDetailsAsync(productId);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteGetProductDetail");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteGetProductsByPriceRange(Dictionary<string, object> @params)
		{
			try
			{
				var minPrice = Convert.ToDecimal(@params["minPrice"]);
				var maxPrice = Convert.ToDecimal(@params["maxPrice"]);
				var result = await _aiAnalyticsService.GetProductsByPriceRangeAsync(minPrice, maxPrice);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteGetProductsByPriceRange");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteAddProductToCart(Dictionary<string, object> @params, int userId)
		{
			try
			{
				var productId = Convert.ToInt32(@params["productId"]);
				var quantity = 1;
				if (@params.ContainsKey("quantity"))
				{
					quantity = Convert.ToInt32(@params["quantity"]);
				}

				var result = await _aiCartService.AddProductToCartAsync(userId, productId, quantity);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteAddProductToCart");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteRemoveProductFromCart(Dictionary<string, object> @params, int userId)
		{
			try
			{
				var productId = Convert.ToInt32(@params["productId"]);
				var result = await _aiCartService.RemoveProductFromCartAsync(userId, productId);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteRemoveProductFromCart");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteGetDoctorsForTreatmentPlan(Dictionary<string, object> @params)
		{
			try
			{
				var treatmentPlanId = Convert.ToInt32(@params["treatmentPlanId"]);
				var result = await _aiAppointmentService.GetDoctorsForTreatmentPlanAsync(treatmentPlanId);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteGetDoctorsForTreatmentPlan");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteGetRecommendedProductsByCategory(Dictionary<string, object> @params)
		{
			try
			{
				if (!@params.ContainsKey("keyword") || string.IsNullOrWhiteSpace(@params["keyword"].ToString()))
				{
					return new AIExecuteToolResponse { Success = false, Error = "Keyword không được để trống" };
				}

				var keyword = @params["keyword"].ToString();
				var result = await _aiAnalyticsService.GetRecommendedProductsByCategoryAsync(keyword);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteGetRecommendedProductsByCategory");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		private async Task<AIExecuteToolResponse> ExecuteGetTreatmentPackagesByServiceName(Dictionary<string, object> @params)
		{
			try
			{
				var serviceName = @params["serviceName"].ToString();
				if (string.IsNullOrWhiteSpace(serviceName))
				{
					return new AIExecuteToolResponse
					{
						Success = false,
						Error = "Tên dịch vụ không được để trống"
					};
				}

				var result = await _aiAnalyticsService.GetTreatmentPackagesByServiceNameAsync(serviceName);
				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ExecuteGetTreatmentPackagesByServiceName");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}


		/// <summary>
		/// 🔥 Main method: Xử lý query từ người dùng
		/// Luồng: Query → LLM → Tool → Result → Response
		/// </summary>
		public async Task<dynamic> ProcessUserQueryAsync(AIFunctionCallRequest request)
		{
			try
			{
				_logger.LogInformation(
					"[STEP 1] Starting ProcessUserQueryAsync - UserId: {UserId}, Query: {Query}",
					request.UserId,
					request.UserQuery);
				var friendlyResponse = CheckFriendlyQuestion(request.UserQuery);
				if (friendlyResponse != null)
				{
					_logger.LogInformation("[FRIENDLY] Matched friendly question: {Query}", request.UserQuery);
					return friendlyResponse;
				}

				// ✅ STEP 1: Lấy tools có sẵn
				var toolsList = await GetAvailableToolsAsync();
				_logger.LogInformation("[STEP 2] Retrieved {ToolCount} tools", toolsList.Tools.Count);

				// ✅ STEP 2: Build system promptXin lỗi,
				string systemPrompt = BuildSystemPrompt();
				_logger.LogInformation("[STEP 3] System prompt built");

				// ✅ STEP 3: Chuẩn bị conversation messages
				var conversationMessages = BuildConversationMessages(
					systemPrompt,
					request.UserQuery,
					request.ConversationHistory);
				_logger.LogInformation("[STEP 4] Conversation messages prepared: {Count}", conversationMessages.Count);

				// ✅ STEP 4: Gọi LLM để xác định tool
				var llmResponseString = await _llmService.CallLLMAsync(
					systemPrompt,
					request.UserQuery,
					conversationMessages);

				_logger.LogInformation(
					"[STEP 5] LLM response received: {Response}",
					llmResponseString);

				// Parse string response to AIFunctionCallResponse
				var llmResponse = ParseLLMResponseString(llmResponseString);

				// 🆕 CHECK 1: Nếu LLM không trả về tool hợp lệ → fallback to chatbot
				if (llmResponse == null || string.IsNullOrWhiteSpace(llmResponse.Tool))
				{
					_logger.LogWarning("[FALLBACK] No valid tool identified from LLM response - Entering chatbot mode");
					return ReturnHotlineResponse(
						"Xin lỗi, tôi không hiểu câu hỏi của bạn hoặc câu hỏi ngoài khả năng hỗ trợ của tôi.",
						request.UserQuery);
				}

				// ✅ CHECK 2: Kiểm tra tool có tồn tại trong danh sách tools không
				var availableToolNames = toolsList.Tools.Select(t => t.Name).ToList();
				if (!availableToolNames.Contains(llmResponse.Tool))
				{
					_logger.LogWarning("[FALLBACK_HOTLINE] Tool '{Tool}' not in available tools list", llmResponse.Tool);
					return ReturnHotlineResponse(
						$"Tôi không có công cụ để xử lý câu hỏi này. Vui lòng liên hệ hotline để được hỗ trợ.",
						request.UserQuery);
				}

				// ✅ Enrich response with IDs (mapping tên → ID)
				llmResponse = await EnrichLLMResponseWithIds(llmResponse);
				_logger.LogInformation("[STEP 5b] LLM response enriched with IDs");

				var toolName = llmResponse.Tool;
				var toolParams = llmResponse.Params ?? new Dictionary<string, object>();

				_logger.LogInformation(
					"[STEP 6] Parsed tool - Tool: {Tool}, Params: {@Params}",
					toolName,
					toolParams);

				// 🔥 AUTO-ENRICH: Thêm customerId từ userId nếu cần (cho tools appointment)
				if ((toolName == "cancelAppointment") &&
					!toolParams.ContainsKey("customerId"))
				{
					toolParams["customerId"] = request.UserId;
					_logger.LogInformation("✅ [AUTO-ENRICH] Added customerId from userId: {CustomerId}", request.UserId);
				}

				// STEP 5: Validate params
				bool isValidParams = await ValidateToolParamsAsync(toolName, toolParams);
				if (!isValidParams)
				{
					_logger.LogWarning("[FALLBACK] Invalid parameters for tool: {Tool}", toolName);
					return ReturnHotlineResponse(
						"Tôi không hiểu yêu cầu của bạn. Vui lòng cung cấp thông tin đầy đủ hoặc liên hệ hotline.",
						request.UserQuery);
				}

				// STEP 6: Execute tool
				var executeRequest = new AIExecuteToolRequest
				{
					UserId = request.UserId,
					LLMResponse = llmResponse
				};

				var toolResult = await ExecuteToolAsync(executeRequest);
				_logger.LogInformation("[STEP 7] Tool executed - Success: {Success}, Error: {Error}", toolResult.Success, toolResult.Error);

				// CHECK 3: Nếu tool không trả về kết quả hoặc data = null
				if (!toolResult.Success || toolResult.Data == null)
				{
					_logger.LogWarning("[FALLBACK] Tool execution failed or returned no data - Error: {Error}", toolResult.Error);
					return ReturnHotlineResponse(
						toolResult.Error ?? "Không tìm thấy kết quả khách hàng mong muốn. Vui lòng liên hệ hotline.",
						request.UserQuery);
				}

				// STEP 7: Format final response (tool thành công)
				var finalResponse = new
				{
					success = toolResult.Success,
					message = toolResult.Error ?? "Thực hiện thành công",
					data = toolResult.Data,
					toolUsed = toolName,
					conversationUpdate = new
					{
						role = "assistant",
						content = FormatResponseMessage(toolResult)
					}
				};

				_logger.LogInformation("[STEP 8] Response formatted and returned");

				return finalResponse;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[ERROR] ProcessUserQueryAsync failed");
				return ReturnHotlineResponse(
					"Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ hotline.",
					request.UserQuery);
			}
		}

		/// <summary>
		/// MỚI: Helper method để trả hotline response
		/// Được gọi khi không có tool phù hợp hoặc tool thất bại
		/// </summary>
		private dynamic ReturnHotlineResponse(string errorMessage, string userQuery)
		{
			try
			{
				_logger.LogWarning("[HOTLINE_RESPONSE] Returning hotline - Query: {Query}, Error: {Error}", userQuery, errorMessage);

				string hotlineMessage = $"❌ {errorMessage}\n\n" +
					$"☎️ <strong>Vui lòng liên hệ hotline của chúng tôi để được hỗ trợ trực tiếp:</strong>\n" +
					$"📞 <strong>0383102388</strong> (Mở cửa 24/7)\n" +
					$"Đội ngũ của chúng tôi sẽ sẵn lòng giúp bạn!";

				dynamic response = new System.Dynamic.ExpandoObject();
				response.success = false;
				response.message = hotlineMessage;
				response.data = null;
				response.toolUsed = "hotline_fallback";
				response.conversationUpdate = new
				{
					role = "assistant",
					content = hotlineMessage
				};

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in ReturnHotlineResponse");
				// Fallback response nếu có lỗi
				return new
				{
					success = false,
					message = "❌ Lỗi hệ thống. Vui lòng liên hệ: 0383102388",
					data = (object)null,
					toolUsed = "error_fallback"
				};
			}
		}

		// ===== PRIVATE HELPER METHODS =====

		private string BuildSystemPrompt()
		{
			return @"Bạn là một trợ lý AI cho hệ thống quản lý phòng khám thẩm mỹ. 
				Nhiệm vụ của bạn là:
					1. Hiểu yêu cầu của người dùng bằng tiếng Việt
					2. Xác định TOOL CHÍNH XÁC cần sử dụng
					3. TRÍCH XUẤT tên bác sĩ, tên dịch vụ từ query (KHÔNG cần ID)
					4. Trả về JSON response

				⚠️ CRITICAL - NHỮNG GÌ KHÔNG PHẢI TOOL CALL:
				- 'Cảm ơn', 'thanks', 'cảm ơn bạn' → KHÔNG gọi tool! Đây là lời cảm ơn - không cần xử lý
				- 'Xin chào', 'hello', 'hi' → KHÔNG gọi tool! Đây là lời chào - không cần xử lý
				- 'Bạn là ai?', 'Bạn tên gì?' → KHÔNG gọi tool! Đây là câu hỏi về mình - không cần xử lý
				- Nếu query không liên quan đến dịch vụ/sản phẩm → KHÔNG gọi tool! Hãy trả về ""Vui lòng liên hệ Hotline 0383102388 để được hỗ trợ. Trân trọng cảm ơn!""

				AVAILABLE TOOLS:
				1. getDoctorAvailableSlots - Lấy lịch trống của bác sĩ trong một ngày
				   Params: {""staffId"": ""Tên bác sĩ hoặc ID bác sĩ"", ""date"": ""YYYY-MM-DD""}

				2. getDoctorAvailableSlotsForTreatmentPlan - Lấy lịch trống cho liệu trình
				   Params: {""staffId"": ""Tên hoặc ID bác sĩ"", ""treatmentPlanId"": int, ""date"": ""YYYY-MM-DD""}

				3. getDoctorsForService - Lấy danh sách bác sĩ của dịch vụ
				   Params: {""serviceId"": ""Tên hoặc ID dịch vụ""}

				4. bookAppointment - Đặt lịch khám (hỗ trợ buổi cụ thể trong liệu trình)
					Params: {""customerId"": int, ""staffId"": ""Tên hoặc ID bác sĩ"", ""serviceId"": ""Tên hoặc ID dịch vụ"", ""appointmentDate"": ""YYYY-MM-DD"", ""appointmentTime"": ""HH:mm"", ""treatmentPlanId"": int (optional), ""sessionNumber"": int (optional)}

				5. cancelAppointment - Hủy lịch hẹn
				   Params: {""customerId"": int, ""staffId"": ""Tên hoặc ID bác sĩ"", ""appointmentDate"": ""YYYY-MM-DD"" (optional), ""serviceId"": ""Tên hoặc ID dịch vụ"" (optional)}

				6. getMostPopularServices - Dịch vụ được đặt lịch nhiều nhất
				   Params: {}

				7. getBestDoctorForService - Bác sĩ tốt nhất của dịch vụ
				   Params: {""serviceId"": ""Tên hoặc ID dịch vụ""}

				8. getServicesByPriceRange - Dịch vụ theo khoảng giá
				   Params: {""minPrice"": decimal, ""maxPrice"": decimal}

				9. getTopSellingProducts - ⭐ Top sản phẩm bán chạy nhất (CÓ THỂ GIỚI HẠN SỐ LƯỢNG)
				   Params: {""limit"": ""int (optional) - Số lượng sản phẩm""}

				10. getProductDetail - Chi tiết sản phẩm + TÁC DỤNG
                    Params: {""productId"": int}
                    ⭐ PHẢI DÙNG khi query hỏi: 'tác dụng của sản phẩm A', 'sản phẩm A có tác dụng gì', 'sản phẩm A giúp gì'

				11. getProductsByPriceRange - Sản phẩm theo khoảng giá
					Params: {""minPrice"": decimal, ""maxPrice"": decimal}

				12. addProductToCart - ⭐⭐⭐ THÊM SẢN PHẨM VÀO GIỎ HÀNG
					Params: {""productId"": int, ""quantity"": int (optional, default: 1)}
					⭐ PHẢI DÙNG khi query có: 'thêm sản phẩm X vào giỏ hàng', 'cho tôi thêm sản phẩm X', 'mua sản phẩm X'

				13. removeProductFromCart - Xóa sản phẩm khỏi giỏ hàng
					Params: {""productId"": int}

				14. getRecommendedProductsByCategory - ⭐ Tư vấn sản phẩm theo yêu cầu/loại (da, mụn, lão hóa, v.v.)
				   Params: {""keyword"": ""Từ khóa tìm kiếm (ví dụ: 'chăm sóc da', 'mụn', 'lão hóa')""}

				15. getTreatmentPackagesByServiceName - ⭐⭐⭐ Lấy thông tin các gói điều trị + buổi điều trị chi tiết
					Params: {""serviceName"": ""Tên dịch vụ/liệu trình (ví dụ: 'trẻ hóa da', 'trị nám')""}

				⚡ CRITICAL DECISION RULES:


				📌 Khi query hỏi ""gói điều trị"", ""liệu trình"", ""buổi điều trị"", ""thông tin các buổi"":
				   → PHẢI DÙNG: getTreatmentPackagesByServiceName (trả về gói + buổi chi tiết)
				   → KHÔNG dùng: getDoctorsForService
				   Ví dụ:
				   - 'Cho tôi thông tin các gói điều trị của liệu trình trẻ hóa da' → getTreatmentPackagesByServiceName
				   - 'Liệu trình trẻ hóa da có những buổi nào?' → getTreatmentPackagesByServiceName
				   - 'Chi tiết các buổi điều trị của dịch vụ trị nám' → getTreatmentPackagesByServiceName
				   - 'Các gói liệu trình của trẻ hóa da' → getTreatmentPackagesByServiceName

				Khi query hỏi ""hủy lịch hẹn buổi N"", ""hủy buổi N"":
				   → PHẢI DÙNG: cancelAppointment
				   → PHẢI TRÍCH XUẤT: sessionNumber từ ""buổi N""
				   → Ví dụ:
				   - 'Hủy lịch hẹn buổi 2' → ""sessionNumber"": 2
				   - 'Hủy buổi 3 của trẻ hóa da' → ""sessionNumber"": 3
				   - 'Hủy lịch buổi 1' → ""sessionNumber"": 1
				   - 'Hủy lịch hẹn buổi 2 gói trẻ hóa da với bác sĩ Ha ngày 25-04-2026' → {""serviceId"": ""trẻ hóa da"", ""staffId"": ""Ha"", ""appointmentDate"": ""2026-04-25"", ""sessionNumber"": 2}


				📌 Khi query hỏi ""danh sách bác sĩ"", ""tất cả bác sĩ"", ""các bác sĩ"":
				   → PHẢI DÙNG: getDoctorsForService (trả về tất cả bác sĩ)
				   → KHÔNG dùng: getBestDoctorForService
				   Ví dụ:
				   - 'Danh sách các bác sĩ của dịch vụ trẻ hóa da' → getDoctorsForService
				   - 'Cho tôi đầy đủ danh sách các bác sĩ của trẻ hóa da' → getDoctorsForService

				📌 Khi query hỏi ""bác sĩ tốt nhất"", ""bác sĩ giỏi nhất"", ""bác sĩ được chọn nhiều nhất"":
				   → PHẢI DÙNG: getBestDoctorForService (chỉ 1 bác sĩ)
				   Ví dụ:
				   - 'Bác sĩ tốt nhất của dịch vụ trẻ hóa da' → getBestDoctorForService

				📌 Khi query hỏi ""tư vấn sản phẩm"", ""sản phẩm về"", ""sản phẩm chăm sóc"":
				   → PHẢI DÙNG: getRecommendedProductsByCategory
				   → TRÍCH XUẤT từ khóa từ query
				   Ví dụ:
				   - 'Tư vấn cho tôi các sản phẩm về mụn' → {""keyword"": ""mụn""}

				📌 Khi query hỏi ""đặt lịch buổi N"", ""buổi thứ N của liệu trình"":
				   → PHẢI TRÍCH XUẤT: treatmentPlanId, sessionNumber (buổi thứ mấy)
				   → TRÍCH XUẤT từ ""buổi N"" → sessionNumber = N
				   Ví dụ:
				   - 'Đặt lịch buổi 3 của liệu trình trẻ hóa da' → {""sessionNumber"": 3, ""treatmentPlanId"": ""trẻ hóa da""}
				   - 'Cho tôi đặt lịch khám buổi 1 của gói liệu trình' → {""sessionNumber"": 1}
				   - 'Buổi 5 của liệu trình này' → {""sessionNumber"": 5}

								📌 ⭐⭐⭐ QUAN TRỌNG: Khi query hỏi ""khoảng giá"", ""dưới"", ""trên"", ""từ X đến Y"":
				   → PHẢI DÙNG: getServicesByPriceRange (cho dịch vụ) hoặc getProductsByPriceRange (cho sản phẩm)
				   → PHẢI TRÍCH XUẤT giá chính xác và tính khoảng ±10%
				   
				   ⚠️ CRITICAL - LOGIC TÍNH KHOẢNG GIÁ:
				   - 'khoảng X' → minPrice = X * 0.9, maxPrice = X * 1.1 (±10%)
				   - 'từ X đến Y' → minPrice = X, maxPrice = Y (exact)
				   - 'dưới X' → minPrice = 0, maxPrice = X
				   - 'trên X' → minPrice = X, maxPrice = 999999999 (very large number)
				   
				   ⚠️ IMPORTANT - KHÔNG bao giờ để minPrice = maxPrice khi query nói ""khoảng""
				   - ❌ SAIÏ: 'khoảng 8 triệu' → minPrice: 8000000, maxPrice: 8000000
				   - ✅ ĐÚNG: 'khoảng 8 triệu' → minPrice: 7200000, maxPrice: 8800000
				   
				   Ví dụ chi tiết:
				   - 'Dịch vụ có giá khoảng 8 triệu' → minPrice: 7200000, maxPrice: 8800000
				   - 'Dịch vụ khoảng 5 triệu' → minPrice: 4500000, maxPrice: 5500000
				   - 'Dịch vụ khoảng 10 triệu' → minPrice: 9000000, maxPrice: 11000000
				   - 'Dịch vụ từ 5 triệu đến 10 triệu' → minPrice: 5000000, maxPrice: 10000000
				   - 'Sản phẩm dưới 1 triệu' → minPrice: 0, maxPrice: 1000000
				   - 'Dịch vụ trên 15 triệu' → minPrice: 15000000, maxPrice: 999999999

				📌 ⭐⭐⭐ QUAN TRỌNG: Khi query hỏi ""Top N"", ""N sản phẩm bán chạy nhất"":
				   → PHẢI DÙNG: getTopSellingProducts
				   → PHẢI TRÍCH XUẤT số N và TRUYỀN vào {""limit"": N}
				   → KHÔNG TRUYỀN limit = không giới hạn, mặc định 10
				   Ví dụ:
				   - 'Top 1 sản phẩm điều trị da' → {""limit"": 1}
				   - 'Top 5 sản phẩm bán chạy nhất' → {""limit"": 5}
				   - 'Top 3 sản phẩm' → {""limit"": 3}
				   - 'Sản phẩm bán chạy nhất' (không có số) → {} (mặc định 3)

				CRITICAL RULES:
				✅ LUÔN trả về JSON với định dạng CHÍNH XÁC:
				{
				  ""tool"": ""toolName"",
				  ""params"": { 
					...parameters...
				  },
				  ""reasoning"": ""Lý do chọn tool này""
				}

				✅ TRÍCH XUẤT TỲ QUERY:
					- Tên bác sĩ ""Toan"" → Truyền như là ""Toan"" (Tên bác sĩ hoặc ID bác sĩ)
					- Tên dịch vụ ""Trị mụn"" → Truyền như là ""Trị mụn""
					- Tên sản phẩm ""Kem Dưỡng Da"" → Truyền như là ""Kem Dưỡng Da"" (KHÔNG cần ID, sẽ tìm tự động)
					- Ngày ""08-04-2026"" → Đổi thành ""2026-04-08"" (YYYY-MM-DD)
					- Ngày ""15/4"" → Đổi thành ""2026-04-15""
					- Giờ ""8h30 sáng"" → Đổi thành ""08:30"" (HH:mm)
					- Giờ ""14h"" → Đổi thành ""14:00"" (HH:mm)
					- Giờ ""3h chiều"" → Đổi thành ""15:00"" (HH:mm - chuyển 24h)
					- Giờ ""8:30"" → Đổi thành ""08:30"" (HH:mm)
					- Buổi ""buổi 2"" → Trích xuất số: 2 → ""sessionNumber"": 2
					- Buổi ""buổi 3 của liệu trình"" → Trích xuất số: 3 → ""sessionNumber"": 3

				✅ RESPONSE EXAMPLE:
				📍 EXAMPLE 0 - User: 'Cho tôi lịch trống của bác sĩ Toan ngày 08-04-2026'
				Response:
				{
				  ""tool"": ""getDoctorAvailableSlots"",
				  ""params"": {
					""staffId"": ""Toan"",
					""date"": ""2026-04-08""
				  },
				  ""reasoning"": ""Người dùng muốn xem lịch trống của bác sĩ Toan vào ngày 08-04-2026""
				}

				📍 EXAMPLE 1 - Danh sách bác sĩ:
				User: 'Cho tôi đầy đủ danh sách các bác sĩ của trẻ hóa da'
				Response:
				{
				  ""tool"": ""getDoctorsForService"",
				  ""params"": {
					""serviceId"": ""trẻ hóa da""
				  },
				  ""reasoning"": ""Người dùng hỏi danh sách/tất cả các bác sĩ của dịch vụ""
				}

				📍 EXAMPLE 2 - Bác sĩ tốt nhất:
				User: 'Bác sĩ tốt nhất của dịch vụ trẻ hóa da là ai?'
				Response:
				{
				  ""tool"": ""getBestDoctorForService"",
				  ""params"": {
					""serviceId"": ""trẻ hóa da""
				  },
				  ""reasoning"": ""Người dùng hỏi bác sĩ tốt nhất → chỉ 1 bác sĩ""
				}

				📍 EXAMPLE 3 - Top 1 sản phẩm:
				User: 'Top 1 sản phẩm điều trị da của của hàng'
				Response:
				{
				  ""tool"": ""getTopSellingProducts"",
				  ""params"": {
					""limit"": 1
				  },
				  ""reasoning"": ""Người dùng hỏi Top 1 sản phẩm → trích xuất số 1 → limit = 1""
				}

				📍 EXAMPLE 4 - Top 5 sản phẩm:
				User: 'Cho tôi top 5 sản phẩm bán chạy nhất'
				Response:
				{
				  ""tool"": ""getTopSellingProducts"",
				  ""params"": {
					""limit"": 5
				  },
				  ""reasoning"": ""Người dùng hỏi top 5 → trích xuất số 5 → limit = 5""
				}

				📍 EXAMPLE 5 - 'Tư vấn cho tôi các sản phẩm về mụn'
				Response:
				{
				  ""tool"": ""getRecommendedProductsByCategory"",
				  ""params"": {
					""keyword"": ""mụn""
				  },
				  ""reasoning"": ""Người dùng tìm sản phẩm về chăm sóc mụn → dùng getRecommendedProductsByCategory""
				}

				📍 EXAMPLE 6 - Đặt lịch buổi với giờ cụ thể:
				User: 'Cho tôi đặt lịch khám buổi 3 của gói Liệu trình trẻ hóa da với bác sĩ Toan ngay 08-04-2026 lúc 8h30 sáng'
				Response:
				{
				  ""tool"": ""bookAppointment"",
				  ""params"": {
					""customerId"": 1,
					""staffId"": ""Toan"",
					""serviceId"": ""Liệu trình trẻ hóa da"",
					""appointmentDate"": ""2026-04-08"",
					""appointmentTime"": ""08:30"",
					""treatmentPlanId"": ""Liệu trình trẻ hóa da"",
					""sessionNumber"": 3
				  },
				  ""reasoning"": ""Người dùng muốn đặt lịch buổi 3 của liệu trình với giờ cụ thể""
				}

				📍 EXAMPLE 7 - Đặt lịch không có buổi:
				User: 'Đặt lịch khám cho tôi với bác sĩ An vào ngày 10-04-2026 lúc 14h'
				Response:
				{
				  ""tool"": ""bookAppointment"",
				  ""params"": {
					""customerId"": 1,
					""staffId"": ""An"",
					""serviceId"": ""<tìm dịch vụ mặc định hoặc hỏi>"",
					""appointmentDate"": ""2026-04-10"",
					""appointmentTime"": ""14:00""
				  },
				  ""reasoning"": ""Người dùng muốn đặt lịch khám với giờ 14h (2h chiều)""
				}

				📍 EXAMPLE 8 - Gói điều trị + buổi chi tiết:
				User: 'Cho tôi thông tin các gói điều trị của liệu trình trẻ hóa da'
				Response:
				{
				  ""tool"": ""getTreatmentPackagesByServiceName"",
				  ""params"": {
					""serviceName"": ""trẻ hóa da""
				  },
				  ""reasoning"": ""Người dùng muốn xem các gói điều trị và buổi chi tiết của liệu trình trẻ hóa da""
				}

				📍 EXAMPLE 9 - Hủy lịch buổi cụ thể:
				User: 'Hủy lịch hẹn buổi 2 gói trị liệu trẻ hóa da với bác sĩ Ha ngày 25-04-2026'
				Response:
				{
				  ""tool"": ""cancelAppointment"",
				  ""params"": {
					""customerId"": 1,
					""staffId"": ""Ha"",
					""serviceId"": ""trẻ hóa da"",
					""appointmentDate"": ""2026-04-25"",
					""sessionNumber"": 2
				  },
				  ""reasoning"": ""Người dùng muốn hủy lịch buổi 2 cụ thể của liệu trình trẻ hóa da vào ngày 25-04-2026 với bác sĩ Ha""
				}

				📍 EXAMPLE 10 - Tác dụng sản phẩm:
                User: 'Tác dụng của Kem Dưỡng Da là gì?'
                Response:
                {
                  ""tool"": ""getProductDetail"",
                  ""params"": {
                    ""productId"": ""Kem Dưỡng Da""
                  },
                  ""reasoning"": ""Người dùng hỏi tác dụng của sản phẩm Kem Dưỡng Da → cần lấy chi tiết sản phẩm bao gồm mô tả, tác dụng, số người sử dụng, kiểm định Bộ Y tế""
                }

								📍 EXAMPLE 11 - Dịch vụ khoảng giá (±10%):
				User: 'Dịch vụ có giá khoảng 8 triệu'
				Response:
				{
				  ""tool"": ""getServicesByPriceRange"",
				  ""params"": {
					""minPrice"": 7200000,
					""maxPrice"": 8800000
				  },
				  ""reasoning"": ""Người dùng tìm dịch vụ trong khoảng giá 8 triệu (tính ±10%: 7.2M - 8.8M)""
				}

				📍 EXAMPLE 12 - Dịch vụ khoảng 5 triệu:
				User: 'Dịch vụ khoảng 5 triệu'
				Response:
				{
				  ""tool"": ""getServicesByPriceRange"",
				  ""params"": {
					""minPrice"": 4500000,
					""maxPrice"": 5500000
				  },
				  ""reasoning"": ""Người dùng tìm dịch vụ khoảng 5 triệu (tính ±10%: 4.5M - 5.5M)""
				}

				📍 EXAMPLE 13 - Dịch vụ từ X đến Y:
				User: 'Dịch vụ từ 5 triệu đến 10 triệu'
				Response:
				{
				  ""tool"": ""getServicesByPriceRange"",
				  ""params"": {
					""minPrice"": 5000000,
					""maxPrice"": 10000000
				  },
				  ""reasoning"": ""Người dùng muốn tìm dịch vụ trong khoảng giá từ 5 triệu đến 10 triệu (exact range)""
				}

				📍 EXAMPLE 14 - Sản phẩm dưới giá:
				User: 'Sản phẩm dưới 1 triệu'
				Response:
				{
				  ""tool"": ""getProductsByPriceRange"",
				  ""params"": {
					""minPrice"": 0,
					""maxPrice"": 1000000
				  },
				  ""reasoning"": ""Người dùng tìm sản phẩm có giá dưới 1 triệu đồng""
				}

				📍 EXAMPLE 15 - Dịch vụ trên giá:
				User: 'Dịch vụ trên 15 triệu'
				Response:
				{
				  ""tool"": ""getServicesByPriceRange"",
				  ""params"": {
					""minPrice"": 15000000,
					""maxPrice"": 999999999
				  },
				  ""reasoning"": ""Người dùng tìm dịch vụ có giá trên 15 triệu đồng""
				}

				Current Date: " + DateTime.UtcNow.ToString("yyyy-MM-dd") + @"
				Current Time: " + DateTime.UtcNow.ToString("HH:mm:ss");
		}

		private List<LLMMessage> BuildConversationMessages(string systemPrompt, string userQuery, List<AIConversationMessage> history)
		{
			var messages = new List<LLMMessage>();

			// System prompt
			messages.Add(new LLMMessage
			{
				Role = "system",
				Content = systemPrompt
			});

			// Conversation history
			if (history != null && history.Count > 0)
			{
				foreach (var msg in history)
				{
					messages.Add(new LLMMessage
					{
						Role = msg.Role,
						Content = msg.Content
					});
				}
			}

			// Current user query
			messages.Add(new LLMMessage
			{
				Role = "user",
				Content = userQuery
			});

			return messages;
		}

		/// <summary>
		/// Parse LLM string response to AIFunctionCallResponse object
		/// </summary>
		private AIFunctionCallResponse ParseLLMResponseString(string llmResponse)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(llmResponse))
				{
					_logger.LogWarning("LLM response is empty");
					return null;
				}

				// Try to parse JSON
				var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(llmResponse);
				
				// Extract tool name
				if (!json.TryGetProperty("tool", out var toolProp))
				{
					_logger.LogWarning("LLM response missing 'tool' property");
					return null;
				}

				var toolName = toolProp.GetString();
				if (string.IsNullOrWhiteSpace(toolName))
				{
					_logger.LogWarning("LLM response has empty 'tool' value");
					return null;
				}

				// Extract params
				var paramDict = new Dictionary<string, object>();
				if (json.TryGetProperty("params", out var paramsProp))
				{
					foreach (var prop in paramsProp.EnumerateObject())
					{
						// Store the value as object - convert types as needed during validation
						if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
						{
							paramDict[prop.Name] = prop.Value.GetString();
						}
						else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
						{
							paramDict[prop.Name] = prop.Value.GetRawText();
						}
						else
						{
							paramDict[prop.Name] = prop.Value.GetRawText();
						}
					}
				}

				// Extract reasoning (optional)
				string reasoning = null;
				if (json.TryGetProperty("reasoning", out var reasoningProp))
				{
					reasoning = reasoningProp.GetString();
				}

				return new AIFunctionCallResponse
				{
					Tool = toolName,
					Params = paramDict,
					Reasoning = reasoning
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error parsing LLM response: {Response}", llmResponse);
				return null;
			}
		}

		private string FormatResponseMessage(AIExecuteToolResponse response)
		{
			if (!response.Success)
				return $"❌ {response.Error}";

			return response.Message ?? "✅ Yêu cầu của bạn đã được xử lý thành công.";
		}

		/// <summary>
		/// Tìm kiếm và thay thế tên bác sĩ/dịch vụ/liệu trình thành ID trong LLM response
		/// </summary>
		/// <summary>
		/// Tìm kiếm và thay thế tên bác sĩ/dịch vụ/liệu trình/sản phẩm thành ID trong LLM response
		/// </summary>
		private async Task<AIFunctionCallResponse> EnrichLLMResponseWithIds(AIFunctionCallResponse llmResponse)
		{
			try
			{
				if (llmResponse?.Params == null)
					return llmResponse;

				var enrichedParams = new Dictionary<string, object>(llmResponse.Params);

				// Nếu có staffId mà là text → tìm kiếm
				if (enrichedParams.ContainsKey("staffId") && enrichedParams["staffId"] != null)
				{
					var staffIdValue = enrichedParams["staffId"].ToString();
					if (!int.TryParse(staffIdValue, out _))
					{
						// Là tên bác sĩ, cần tìm ID
						var staffId = await FindStaffIdByNameAsync(staffIdValue);
						if (staffId.HasValue)
						{
							enrichedParams["staffId"] = staffId.Value;
							_logger.LogInformation("✓ Mapped staff name '{Name}' to staffId: {StaffId}", staffIdValue, staffId.Value);
						}
						else
						{
							_logger.LogWarning("⚠ Could not find staff ID for name: {Name}", staffIdValue);
						}
					}
				}

				// Nếu có serviceId mà là text → tìm kiếm
				if (enrichedParams.ContainsKey("serviceId") && enrichedParams["serviceId"] != null)
				{
					var serviceIdValue = enrichedParams["serviceId"].ToString();
					if (!int.TryParse(serviceIdValue, out _))
					{
						// Là tên dịch vụ, cần tìm ID
						var serviceId = await FindServiceIdByNameAsync(serviceIdValue);
						if (serviceId.HasValue)
						{
							enrichedParams["serviceId"] = serviceId.Value;
							_logger.LogInformation("✓ Mapped service name '{Name}' to serviceId: {ServiceId}", serviceIdValue, serviceId.Value);
						}
						else
						{
							_logger.LogWarning("⚠ Could not find service ID for name: {Name}", serviceIdValue);
						}
					}
				}

				// ✅ Nếu có treatmentPlanId mà là text → tìm kiếm
				if (enrichedParams.ContainsKey("treatmentPlanId") && enrichedParams["treatmentPlanId"] != null)
				{
					var treatmentPlanIdValue = enrichedParams["treatmentPlanId"].ToString();
					if (!int.TryParse(treatmentPlanIdValue, out _))
					{
						// Là tên liệu trình, cần tìm ID
						_logger.LogInformation("🔍 Looking for treatment plan: '{Name}'", treatmentPlanIdValue);
						var treatmentPlanId = await FindTreatmentPlanIdByNameAsync(treatmentPlanIdValue);
						if (treatmentPlanId.HasValue)
						{
							enrichedParams["treatmentPlanId"] = treatmentPlanId.Value;
							_logger.LogInformation("✓ Mapped treatment plan name '{Name}' to treatmentPlanId: {TreatmentPlanId}", treatmentPlanIdValue, treatmentPlanId.Value);
						}
						else
						{
							_logger.LogWarning("⚠ Could not find treatment plan ID for name: {Name}. Available plans will be logged.", treatmentPlanIdValue);
							// Log all available treatment plans for debugging
							await LogAvailableTreatmentPlans();
						}
					}
				}

				// 🆕 Nếu có productId mà là text → tìm kiếm
				if (enrichedParams.ContainsKey("productId") && enrichedParams["productId"] != null)
				{
					var productIdValue = enrichedParams["productId"].ToString();
					if (!int.TryParse(productIdValue, out _))
					{
						// Là tên sản phẩm, cần tìm ID
						_logger.LogInformation("🔍 Looking for product: '{Name}'", productIdValue);
						var productId = await FindProductIdByNameAsync(productIdValue);
						if (productId.HasValue)
						{
							enrichedParams["productId"] = productId.Value;
							_logger.LogInformation("✓ Mapped product name '{Name}' to productId: {ProductId}", productIdValue, productId.Value);
						}
						else
						{
							_logger.LogWarning("⚠ Could not find product ID for name: {Name}", productIdValue);
							await LogAvailableProducts(productIdValue);
						}
					}
				}

				return new AIFunctionCallResponse
				{
					Tool = llmResponse.Tool,
					Params = enrichedParams,
					Reasoning = llmResponse.Reasoning
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error enriching LLM response with IDs");
				return llmResponse;
			}
		}

		// 🆕 Method tìm productId theo tên
		private async Task<int?> FindProductIdByNameAsync(string productName)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(productName))
					return null;

				var products = await _productRepository.FindByPredicate(x =>
					!x.DeleteStatus);

				_logger.LogInformation("📦 Total active products: {Count}", products.Count());

				// Tìm kiếm chính xác
				var exactMatch = products.FirstOrDefault(p =>
					p.ProductName?.Equals(productName, StringComparison.OrdinalIgnoreCase) == true);

				if (exactMatch != null)
				{
					_logger.LogInformation("✓ Exact match found: '{Name}' → ID: {Id}", exactMatch.ProductName, exactMatch.Id);
					return exactMatch.Id;
				}

				// Tìm kiếm gần đúng (contains)
				var fuzzyMatch = products.FirstOrDefault(p =>
					p.ProductName?.Contains(productName, StringComparison.OrdinalIgnoreCase) == true);

				if (fuzzyMatch != null)
				{
					_logger.LogInformation("✓ Fuzzy match found: '{Name}' contains '{Search}' → ID: {Id}", fuzzyMatch.ProductName, productName, fuzzyMatch.Id);
					return fuzzyMatch.Id;
				}

				_logger.LogWarning("❌ No match found for product: '{Name}'", productName);
				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error finding product ID for name: {Name}", productName);
				return null;
			}
		}

		// 🆕 Helper method để log tất cả available products
		private async Task LogAvailableProducts(string searchedName)
		{
			try
			{
				var products = await _productRepository.FindByPredicate(x => !x.DeleteStatus);
				if (!products.Any())
				{
					_logger.LogWarning("No active products found in database");
					return;
				}

				_logger.LogInformation("📦 Available Products (searched for: '{Search}'):", searchedName);
				foreach (var product in products.Take(10))
				{
					_logger.LogInformation("   - ID: {Id}, Name: '{Name}'", product.Id, product.ProductName);
				}

				if (products.Count() > 10)
				{
					_logger.LogInformation("   ... và {Count} sản phẩm khác", products.Count() - 10);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error logging available products");
			}
		}
		private async Task<int?> FindStaffIdByNameAsync(string staffName)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(staffName))
					return null;

				var staffs = await _staffRepository.FindByPredicate(x =>
					!x.DeleteStatus && x.IsDoctor == true);

				// Tìm kiếm chính xác
				var exactMatch = staffs.FirstOrDefault(s =>
					s.FullName?.Equals(staffName, StringComparison.OrdinalIgnoreCase) == true);

				if (exactMatch != null)
					return exactMatch.Id;

				// Tìm kiếm gần đúng (contains)
				var fuzzyMatch = staffs.FirstOrDefault(s =>
					s.FullName?.Contains(staffName, StringComparison.OrdinalIgnoreCase) == true);

				if (fuzzyMatch != null)
					return fuzzyMatch.Id;

				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error finding staff ID for name: {Name}", staffName);
				return null;
			}
		}

		private async Task<int?> FindServiceIdByNameAsync(string serviceName)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(serviceName))
					return null;

				var services = await _serviceRepository.FindByPredicate(x =>
					!x.DeleteStatus);

				// Tìm kiếm chính xác
				var exactMatch = services.FirstOrDefault(s =>
					s.ServiceName?.Equals(serviceName, StringComparison.OrdinalIgnoreCase) == true);

				if (exactMatch != null)
					return exactMatch.Id;

				// Tìm kiếm gần đúng (contains)
				var fuzzyMatch = services.FirstOrDefault(s =>
					s.ServiceName?.Contains(serviceName, StringComparison.OrdinalIgnoreCase) == true);

				if (fuzzyMatch != null)
					return fuzzyMatch.Id;

				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error finding service ID for name: {Name}", serviceName);
				return null;
			}
		}

		// ✅ Method tìm treatmentPlanId theo tên
		private async Task<int?> FindTreatmentPlanIdByNameAsync(string treatmentPlanName)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(treatmentPlanName))
					return null;

				var treatmentPlans = await _treatmentPlanRepository.FindByPredicate(x =>
					!x.DeleteStatus);

				_logger.LogInformation("📋 Total active treatment plans: {Count}", treatmentPlans.Count());

				// Tìm kiếm chính xác
				var exactMatch = treatmentPlans.FirstOrDefault(t =>
					t.PlanName?.Equals(treatmentPlanName, StringComparison.OrdinalIgnoreCase) == true);

				if (exactMatch != null)
				{
					_logger.LogInformation("✓ Exact match found: '{Name}' → ID: {Id}", exactMatch.PlanName, exactMatch.Id);
					return exactMatch.Id;
				}

				// Tìm kiếm gần đúng (contains)
				var fuzzyMatch = treatmentPlans.FirstOrDefault(t =>
					t.PlanName?.Contains(treatmentPlanName, StringComparison.OrdinalIgnoreCase) == true);

				if (fuzzyMatch != null)
				{
					_logger.LogInformation("✓ Fuzzy match found: '{Name}' contains '{Search}' → ID: {Id}", fuzzyMatch.PlanName, treatmentPlanName, fuzzyMatch.Id);
					return fuzzyMatch.Id;
				}

				_logger.LogWarning("❌ No match found for treatment plan: '{Name}'", treatmentPlanName);
				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error finding treatment plan ID for name: {Name}", treatmentPlanName);
				return null;
			}
		}

		// ✅ Helper method để log tất cả available treatment plans
		private async Task LogAvailableTreatmentPlans()
		{
			try
			{
				var plans = await _treatmentPlanRepository.FindByPredicate(x => !x.DeleteStatus);
				if (!plans.Any())
				{
					_logger.LogWarning("No active treatment plans found in database");
					return;
				}

				_logger.LogInformation("📋 Available Treatment Plans:");
				foreach (var plan in plans)
				{
					_logger.LogInformation("   - ID: {Id}, Name: '{Name}'", plan.Id, plan.PlanName);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error logging available treatment plans");
			}
		}

		#region Validate

		private bool ValidateDoctorSlotsParams(Dictionary<string, object> @params)
			=> @params.ContainsKey("staffId") && @params.ContainsKey("date");

		private bool ValidateDoctorTreatmentSlotsParams(Dictionary<string, object> @params)
			=> @params.ContainsKey("staffId") && @params.ContainsKey("treatmentPlanId") && @params.ContainsKey("date");

		private bool ValidateServiceParams(Dictionary<string, object> @params)
			=> @params.ContainsKey("serviceId");

		private bool ValidateBookingParams(Dictionary<string, object> @params)
			=> @params.ContainsKey("customerId") &&
			   @params.ContainsKey("staffId") &&
			   @params.ContainsKey("serviceId") &&
			   @params.ContainsKey("appointmentDate") &&
			   @params.ContainsKey("appointmentTime");

		private bool ValidateCancelParams(Dictionary<string, object> @params)
		{
			// Kiểm tra staffId có tồn tại (BẮT BUỘC)
			if (!@params.ContainsKey("staffId") || @params["staffId"] == null)
			{
				_logger.LogWarning("ValidateCancelParams: Missing staffId");
				return false;
			}

			// ✅ staffId có thể là số hoặc tên → chỉ kiểm tra không rỗng
			var staffIdValue = @params["staffId"].ToString();
			if (string.IsNullOrWhiteSpace(staffIdValue))
			{
				_logger.LogWarning("ValidateCancelParams: staffId is empty");
				return false;
			}

			// ✅ customerId là OPTIONAL (có thể được thêm từ userId sau)
			if (@params.ContainsKey("customerId") && @params["customerId"] != null)
			{
				if (!int.TryParse(@params["customerId"].ToString(), out _))
				{
					_logger.LogWarning("ValidateCancelParams: Invalid customerId format: {CustomerId}",
						@params["customerId"]);
					return false;
				}
			}

			// ✅ Kiểm tra optional parameters
			if (@params.ContainsKey("appointmentDate") && @params["appointmentDate"] != null)
			{
				var dateStr = @params["appointmentDate"].ToString();
				if (!DateTime.TryParse(dateStr, out _))
				{
					_logger.LogWarning("ValidateCancelParams: Invalid appointmentDate format: {Date}", dateStr);
					return false;
				}
			}

			if (@params.ContainsKey("serviceId") && @params["serviceId"] != null)
			{
				var serviceIdValue = @params["serviceId"].ToString();
				if (string.IsNullOrWhiteSpace(serviceIdValue))
				{
					_logger.LogWarning("ValidateCancelParams: serviceId is empty");
					return false;
				}
			}

			_logger.LogInformation("✓ ValidateCancelParams passed all checks");
			return true;
		}

		private bool ValidateTreatmentPlanParams(Dictionary<string, object> @params)
			=> @params.ContainsKey("treatmentPlanId");

		private bool ValidatePriceRangeParams(Dictionary<string, object> @params)
			=> @params.ContainsKey("minPrice") && @params.ContainsKey("maxPrice");

		private bool ValidateProductParams(Dictionary<string, object> @params)
			=> @params.ContainsKey("productId");

		private bool ValidateAddToCartParams(Dictionary<string, object> @params)
			=> @params.ContainsKey("productId");

		private bool ValidateKeywordParams(Dictionary<string, object> @params)
			=> @params.ContainsKey("keyword") && !string.IsNullOrWhiteSpace(@params["keyword"].ToString());

		private bool ValidateServiceNameParams(Dictionary<string, object> @params)
		{
			if (@params == null || !@params.ContainsKey("serviceName"))
				return false;

			var serviceName = @params["serviceName"]?.ToString();
			return !string.IsNullOrWhiteSpace(serviceName);
		}

		private bool ValidateAvailableSlotsForServiceParams(Dictionary<string, object> @params)
		{
			return @params.ContainsKey("serviceId") &&
				   int.TryParse(@params["serviceId"].ToString(), out _) &&
				   @params.ContainsKey("date") &&
				   DateTime.TryParse(@params["date"].ToString(), out _);
		}

		#endregion

		#region Funciton private cancel appointment

		/// <summary>
		/// CASE 1: Hủy TẤT CẢ lịch hẹn của khách hàng với bác sĩ
		/// Query: "Hủy hẹn với bác sĩ A"
		/// </summary>
		/// 
		private async Task<AIExecuteToolResponse> HandleCancelAllAppointmentsWithDoctor(int customerId, int staffId)
		{
			try
			{
				_logger.LogInformation("🔍 CASE 1: Fetching all appointments - CustomerId: {CustomerId}, StaffId: {StaffId}",
					customerId, staffId);

				var result = await _aiAppointmentService.CancelAppointmentAsync(customerId, staffId, null, null);

				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ Error in HandleCancelAllAppointmentsWithDoctor");
				return CreateErrorResponse(ex.Message);
			}
		}

		/// <summary>
		/// CASE 2: Hủy lịch hẹn của khách hàng với bác sĩ vào NGÀY CỤ THỂ (tất cả dịch vụ)
		/// Query: "Hủy lịch hẹn với bác sĩ A ngày B"
		/// </summary>
		private async Task<AIExecuteToolResponse> HandleCancelAppointmentsByDate(int customerId, int staffId, DateTime appointmentDate)
		{
			try
			{
				_logger.LogInformation(
					"🔍 CASE 2: Fetching appointments by date - CustomerId: {CustomerId}, StaffId: {StaffId}, Date: {Date}",
					customerId, staffId, appointmentDate.ToString("yyyy-MM-dd"));

				var result = await _aiAppointmentService.CancelAppointmentAsync(
					customerId,
					staffId,
					appointmentDate,
					null);

				return new AIExecuteToolResponse
				{
					Success = result.Success,
					Data = result,
					Message = result.Message,
					Error = result.Success ? null : result.Message
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ Error in HandleCancelAppointmentsByDate");
				return CreateErrorResponse(ex.Message);
			}
		}

		/// <summary>
		/// CASE 3: Hủy lịch hẹn của khách hàng với bác sĩ cho DỊCH VỤ CỤ THỂ
		/// Xử lý 2 sub-case:
		/// 3a. Nếu C là DỊCH VỤ ĐƠN LẺ → Hủy tất cả lịch hẹn của dịch vụ đó
		/// 3b. Nếu C là GÓI LIỆU TRÌNH → Hủy tất cả lịch hẹn của tất cả buổi trong liệu trình
		/// Query: "Hủy lịch hẹn với bác sĩ A dịch vụ C"
		/// </summary>
		private async Task<AIExecuteToolResponse> HandleCancelAppointmentsByService(int customerId, int staffId, int serviceId)
		{
			try
			{
				_logger.LogInformation(
					"🔍 CASE 3: Checking if service is course (treatment plan) - ServiceId: {ServiceId}",
					serviceId);

				// Kiểm tra xem dịch vụ có phải liệu trình hay không
				var service = await _serviceRepository.GetById(serviceId);
				if (service == null)
				{
					return CreateErrorResponse($"Không tìm thấy dịch vụ ID {serviceId}");
				}

				_logger.LogInformation("✓ Service found: {ServiceName}, IsCourse: {IsCourse}",
					service.ServiceName, service.IsCourse);

				// CASE 3a: Dịch vụ đơn lẻ → Hủy tất cả lịch hẹn của dịch vụ đó
				if (service.IsCourse != true)
				{
					_logger.LogInformation("📍 CASE 3a: Service is NOT a course (single service) → Hủy tất cả lịch hẹn của dịch vụ {ServiceId}",
						serviceId);

					var result = await _aiAppointmentService.CancelAppointmentAsync(
						customerId,
						staffId,
						null,
						serviceId);

					return new AIExecuteToolResponse
					{
						Success = result.Success,
						Data = result,
						Message = result.Message,
						Error = result.Success ? null : result.Message
					};
				}

				// CASE 3b: Gói liệu trình → Hủy tất cả buổi đã đặt lịch của liệu trình này
				_logger.LogInformation("📍 CASE 3b: Service IS a course (treatment plan) → Hủy tất cả buổi của liệu trình {ServiceId}",
					serviceId);

				return await HandleCancelTreatmentPlanAppointments(customerId, staffId, serviceId, null);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ Error in HandleCancelAppointmentsByService");
				return CreateErrorResponse(ex.Message);
			}
		}

		/// <summary>
		/// CASE 4: Hủy lịch hẹn của khách hàng với bác sĩ vào NGÀY CỤ THỀ cho DỊCH VỤ CỤ THỂ
		/// Xử lý 2 sub-case:
		/// 4a. Nếu C là DỊCH VỤ ĐƠN LẺ → Hủy lịch hẹn của dịch vụ đó vào ngày cụ thể
		/// 4b. Nếu C là GÓI LIỆU TRÌNH → Hủy tất cả lịch hẹn buổi của liệu trình vào ngày cụ thể
		/// Query: "Hủy lịch hẹn với bác sĩ A ngày B dịch vụ C"
		/// </summary>
		private async Task<AIExecuteToolResponse> HandleCancelAppointmentsByDateAndService(int customerId, int staffId, DateTime appointmentDate, int serviceId)
		{
			try
			{
				_logger.LogInformation(
					"🔍 CASE 4: Checking if service is course - ServiceId: {ServiceId}",
					serviceId);

				// Kiểm tra xem dịch vụ có phải liệu trình hay không
				var service = await _serviceRepository.GetById(serviceId);
				if (service == null)
				{
					return CreateErrorResponse($"Không tìm thấy dịch vụ ID {serviceId}");
				}

				_logger.LogInformation("✓ Service found: {ServiceName}, IsCourse: {IsCourse}",
					service.ServiceName, service.IsCourse);

				// CASE 4a: Dịch vụ đơn lẻ → Hủy lịch hẹn của dịch vụ đó vào ngày cụ thể
				if (service.IsCourse != true)
				{
					_logger.LogInformation(
						"📍 CASE 4a: Service is NOT a course → Hủy lịch hẹn ngày {Date} dịch vụ {ServiceId}",
						appointmentDate.ToString("yyyy-MM-dd"), serviceId);

					var result = await _aiAppointmentService.CancelAppointmentAsync(
						customerId,
						staffId,
						appointmentDate,
						serviceId);

					return new AIExecuteToolResponse
					{
						Success = result.Success,
						Data = result,
						Message = result.Message,
						Error = result.Success ? null : result.Message
					};
				}

				// CASE 4b: Gói liệu trình → Hủy tất cả buổi của liệu trình vào ngày cụ thể
				_logger.LogInformation(
					"📍 CASE 4b: Service IS a course → Hủy buổi liệu trình ngày {Date}",
					appointmentDate.ToString("yyyy-MM-dd"));

				return await HandleCancelTreatmentPlanAppointments(customerId, staffId, serviceId, appointmentDate);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ Error in HandleCancelAppointmentsByDateAndService");
				return CreateErrorResponse(ex.Message);
			}
		}

		/// <summary>
		/// Hủy tất cả lịch hẹn buổi của liệu trình
		/// Nếu có appointmentDate → chỉ hủy buổi vào ngày đó
		/// Nếu không có appointmentDate → hủy tất cả buổi
		/// </summary>
		private async Task<AIExecuteToolResponse> HandleCancelTreatmentPlanAppointments(int customerId, int staffId, int serviceId, DateTime? appointmentDate)
		{
			try
			{
				_logger.LogInformation(
					"🔍 Looking up treatment plans for service {ServiceId}",
					serviceId);

				// Lấy tất cả treatment plans của service
				var treatmentPlans = await _treatmentPlanRepository.FindByPredicate(x =>
					x.ServiceId == serviceId && !x.DeleteStatus);

				if (!treatmentPlans.Any())
				{
					_logger.LogWarning("⚠ No treatment plans found for service {ServiceId}", serviceId);
					return CreateErrorResponse($"Không tìm thấy liệu trình nào cho dịch vụ ID {serviceId}");
				}

				_logger.LogInformation("✓ Found {Count} treatment plans", treatmentPlans.Count());

				int totalCancelledCount = 0;
				var cancelledDetails = new List<string>();

				// Hủy lịch hẹn cho mỗi treatment plan
				foreach (var plan in treatmentPlans)
				{
					_logger.LogInformation("🔍 Processing treatment plan: {PlanName} (ID: {PlanId})",
						plan.PlanName, plan.Id);

					// Gọi CancelAppointmentAsync với serviceId
					// Nếu có appointmentDate → chỉ hủy lịch hẹn vào ngày đó
					var result = await _aiAppointmentService.CancelAppointmentAsync(
						customerId,
						staffId,
						appointmentDate,
						serviceId);

					if (result.Success)
					{
						totalCancelledCount += result.CancelledCount;
						cancelledDetails.Add($"{plan.PlanName}: {result.CancelledCount} buổi");
						_logger.LogInformation("✓ Cancelled {Count} appointments for plan {PlanName}",
							result.CancelledCount, plan.PlanName);
					}
					else
					{
						_logger.LogWarning("⚠ Failed to cancel appointments for plan {PlanName}: {Message}",
							plan.PlanName, result.Message);
					}
				}

				if (totalCancelledCount == 0)
				{
					return CreateErrorResponse("Không tìm thấy lịch hẹn nào để hủy cho liệu trình này");
				}

				var finalMessage = $"✅ Đã hủy {totalCancelledCount} buổi liệu trình:\n" +
					string.Join("\n", cancelledDetails.Select(d => $"  • {d}"));

				return new AIExecuteToolResponse
				{
					Success = true,
					Data = new { CancelledCount = totalCancelledCount, Details = cancelledDetails },
					Message = finalMessage,
					Error = null
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ Error in HandleCancelTreatmentPlanAppointments");
				return CreateErrorResponse(ex.Message);
			}
		}

		/// <summary>
		/// Helper method để tạo error response
		/// </summary>
		private AIExecuteToolResponse CreateErrorResponse(string error)
		{
			_logger.LogError("❌ Error: {Error}", error);
			return new AIExecuteToolResponse
			{
				Success = false,
				Error = error,
				Message = error
			};
		}

		/// <summary>
		/// CASE SPECIAL: Hủy buổi cụ thể (buổi N) của liệu trình
		/// Query: "Hủy lịch hẹn buổi 2 gói trẻ hóa da với bác sĩ A ngày B"
		/// 
		/// Luồng xử lý:
		/// 1. Lấy TreatmentPlans của serviceId
		/// 2. Lấy TreatmentSessions của TreatmentPlan → tìm SessionNumber = 2
		/// 3. Lấy CustomerTreatmentSessions tương ứng
		/// 4. Hủy Appointment dựa trên CustomerTreatmentSession
		/// </summary>
		private async Task<AIExecuteToolResponse> HandleCancelSpecificSessionAppointment(int customerId, int staffId, int serviceId, int sessionNumber, DateTime? appointmentDate)
		{
			try
			{
				_logger.LogInformation(
					"🔍 CASE SPECIAL: Starting cancel specific session - Service: {ServiceId}, Session: {SessionNumber}, Date: {Date}",
					serviceId, sessionNumber, appointmentDate?.ToString("yyyy-MM-dd"));

				// ✅ STEP 1: Lấy TreatmentPlans của service
				var treatmentPlans = await _treatmentPlanRepository.FindByPredicate(x =>
					x.ServiceId == serviceId && !x.DeleteStatus);

				if (!treatmentPlans.Any())
				{
					return CreateErrorResponse($"Không tìm thấy liệu trình nào cho dịch vụ ID {serviceId}");
				}

				_logger.LogInformation("✓ Found {Count} treatment plans for service {ServiceId}",
					treatmentPlans.Count(), serviceId);

				int totalCancelledCount = 0;
				int totalAssignmentCount = 0; // 🆕 Đếm assignment bị hủy
				var cancelledDetails = new List<string>();

				// ✅ STEP 2: Xử lý từng treatment plan
				foreach (var treatmentPlan in treatmentPlans)
				{
					_logger.LogInformation(
						"🔍 Processing treatment plan: {PlanName} (ID: {PlanId})",
						treatmentPlan.PlanName, treatmentPlan.Id);

					// ✅ STEP 2a: Lấy TreatmentSessions của treatment plan
					var treatmentSessions = await _treatmentSessionRepository.FindByPredicate(x =>
						x.TreatmentPlanId == treatmentPlan.Id && !x.DeleteStatus);

					_logger.LogInformation("✓ Found {Count} treatment sessions in plan", treatmentSessions.Count());

					// ✅ STEP 2b: Tìm TreatmentSession có SessionNumber = sessionNumber
					var targetSession = treatmentSessions.FirstOrDefault(x => x.SessionNumber == sessionNumber);
					if (targetSession == null)
					{
						_logger.LogWarning(
							"⚠ Session #{SessionNumber} not found in plan {PlanName}",
							sessionNumber, treatmentPlan.PlanName);
						continue;
					}

					_logger.LogInformation(
						"✓ Found target session: {SessionName} (SessionNumber: {Number}, ID: {SessionId})",
						targetSession.SessionName, sessionNumber, targetSession.Id);

					// ✅ STEP 2c: Lấy CustomerTreatmentPlan của khách hàng cho liệu trình này
					var customerTreatmentPlan = await _customerTreatmentPlansRepository.FindByPredicate(x =>
						x.CustomerId == customerId &&
						x.TreatmentPlanId == treatmentPlan.Id &&
						!x.DeleteStatus);

					if (!customerTreatmentPlan.Any())
					{
						_logger.LogWarning(
							"⚠ No CustomerTreatmentPlan found for customer {CustomerId}, plan {PlanId}",
							customerId, treatmentPlan.Id);
						continue;
					}

					_logger.LogInformation(
						"✓ Found {Count} CustomerTreatmentPlan(s)",
						customerTreatmentPlan.Count());

					// ✅ STEP 2d: Lấy CustomerTreatmentSession tương ứng
					foreach (var custPlan in customerTreatmentPlan)
					{
						var customerTreatmentSessions = await _customerTreatmentSessionsRepository.FindByPredicate(x =>
							x.CustomerTreatmentPlanId == custPlan.Id &&
							x.TreatmentSessionId == targetSession.Id &&
							!x.DeleteStatus);

						if (!customerTreatmentSessions.Any())
						{
							_logger.LogWarning(
								"⚠ No CustomerTreatmentSession found for session {SessionId}",
								targetSession.Id);
							continue;
						}

						_logger.LogInformation(
							"✓ Found {Count} CustomerTreatmentSession(s) to cancel",
							customerTreatmentSessions.Count());

						// ✅ STEP 2e: Hủy Appointments dựa trên CustomerTreatmentSession
						foreach (var custSession in customerTreatmentSessions)
						{
							// Tìm Appointments liên kết với CustomerTreatmentSession này
							var appointments = await _appointmentRepository.FindByPredicate(x =>
								x.CustomerId == customerId &&
								x.StaffId == staffId &&
								x.CustomerTreatmentSessionId == custSession.Id &&
								x.Status != (int)AppointmentStatus.Cancelled &&
								!x.DeleteStatus);

							// Lọc theo ngày nếu có
							if (appointmentDate.HasValue)
							{
								appointments = appointments
									.Where(x => x.StartTime!.Value.Date == appointmentDate.Value.Date)
									.ToList();
							}

							_logger.LogInformation(
								"🔍 Found {Count} appointments to cancel for session {SessionNumber}",
								appointments.Count(), sessionNumber);

							// Hủy từng appointment + assignment
							foreach (var apt in appointments)
							{
								try
								{
									_logger.LogInformation("📅 Cancelling appointment ID={Id}, Time={Time}", apt.Id, apt.StartTime);

									// ✅ STEP 3.1: Update status của AppointmentAssignments (KHÔNG XÓA)
									var assignments = await _appointmentAssignmentRepository.FindByPredicate(x =>
										x.AppointmentId == apt.Id && !x.DeleteStatus);

									if (assignments.Any())
									{
										_logger.LogInformation("🔗 Found {Count} AppointmentAssignment(s) to cancel", assignments.Count());

										foreach (var assignment in assignments)
										{
											try
											{
												// 🆕 Chỉ update status, KHÔNG xóa
												assignment.Status = (int)AppointmentStatus.Cancelled;
												// assignment.DeleteStatus = true; ❌ KHÔNG set DeleteStatus

												var assignmentUpdated = await _appointmentAssignmentRepository.UpdateEntity(assignment);
												if (assignmentUpdated)
												{
													totalAssignmentCount++;
													_logger.LogInformation("✓ AppointmentAssignment status updated: ID={Id}, Status=Cancelled", assignment.Id);
												}
												else
												{
													_logger.LogWarning("⚠ Failed to update AppointmentAssignment status: ID={Id}", assignment.Id);
												}
											}
											catch (Exception ex)
											{
												_logger.LogError(ex, "❌ Error updating AppointmentAssignment: ID={Id}", assignment.Id);
											}
										}
									}
									else
									{
										_logger.LogInformation("ℹ No AppointmentAssignments found for appointment {AppointmentId}", apt.Id);
									}

									// ✅ STEP 3.2: Hủy Appointment
									apt.Status = (int)AppointmentStatus.Cancelled;
									// apt.DeleteStatus = true; ❌ KHÔNG set DeleteStatus để giữ audit trail

									var updated = await _appointmentRepository.UpdateEntity(apt);
									if (updated)
									{
										totalCancelledCount++;
										cancelledDetails.Add(
											$"Buổi {sessionNumber}: {apt.StartTime:dd/MM/yyyy HH:mm}");
										_logger.LogInformation(
											"✓ Appointment cancelled: ID={Id}, Time={Time}",
											apt.Id, apt.StartTime);
									}
									else
									{
										_logger.LogWarning("⚠ Failed to cancel Appointment: ID={Id}", apt.Id);
									}
								}
								catch (Exception ex)
								{
									_logger.LogError(ex, "❌ Error in appointment cancellation flow: ID={Id}", apt.Id);
								}
							}

							// 🆕 STEP 3.3: Update CustomerTreatmentSession status thành "KhachHuy"
							try
							{
								custSession.Status = "KhachHuy";
								var sessionUpdated = await _customerTreatmentSessionsRepository.UpdateEntity(custSession);
								if (sessionUpdated)
								{
									_logger.LogInformation("✓ CustomerTreatmentSession status updated: ID={Id}, Status=KhachHuy", custSession.Id);

									// 🆕 STEP 3.4: Check và update CustomerTreatmentPlan status nếu cần
									if (custPlan.Id > 0)
									{
										var allSessions = await _customerTreatmentSessionsRepository.FindByPredicate(x =>
											x.CustomerTreatmentPlanId == custPlan.Id &&
											!x.DeleteStatus);

										// Nếu TẤT CẢ sessions = "KhachHuy" → Plan = "KhachHuy"
										if (allSessions.All(s => s.Status == "KhachHuy"))
										{
											custPlan.Status = "KhachHuy";
											var planUpdated = await _customerTreatmentPlansRepository.UpdateEntity(custPlan);
											if (planUpdated)
											{
												_logger.LogInformation("✓ CustomerTreatmentPlan status updated to KhachHuy: ID={Id}", custPlan.Id);
											}
										}
										else
										{
											_logger.LogInformation("ℹ Plan has other active sessions, status not changed: ID={Id}", custPlan.Id);
										}
									}
								}
								else
								{
									_logger.LogWarning("⚠ Failed to update CustomerTreatmentSession status: ID={Id}", custSession.Id);
								}
							}
							catch (Exception ex)
							{
								_logger.LogError(ex, "❌ Error updating session/plan status: ID={Id}", custSession.Id);
							}
						}
					}
				}

				if (totalCancelledCount == 0)
				{
					return CreateErrorResponse(
						$"Không tìm thấy lịch hẹn nào để hủy cho buổi {sessionNumber}");
				}

				var finalMessage = $"✅ Đã hủy {totalCancelledCount} lịch hẹn buổi {sessionNumber}" +
					(totalAssignmentCount > 0 ? $" | Cập nhật {totalAssignmentCount} assignment(s)" : "") +
					":\n" + string.Join("\n", cancelledDetails.Select(d => $"  • {d}"));

				return new AIExecuteToolResponse
				{
					Success = true,
					Data = new { CancelledCount = totalCancelledCount, AssignmentCount = totalAssignmentCount, Details = cancelledDetails },
					Message = finalMessage,
					Error = null
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ Error in HandleCancelSpecificSessionAppointment");
				return CreateErrorResponse(ex.Message);
			}
		}

		private dynamic CheckFriendlyQuestion(string userQuery)
		{
			if (string.IsNullOrWhiteSpace(userQuery))
				return null;

			var lowerQuery = userQuery.ToLower().Trim();

			var friendlyResponses = new Dictionary<string, string>
			{
				// ===== LỜI CHÀO =====
				{ "xin chào", "👋 Xin chào bạn! Mình là trợ lý AI của phòng khám thẩm mỹ. Mình có thể giúp bạn tìm kiếm dịch vụ, đặt lịch, hoặc trò chuyện cùng bạn. Bạn cần gì nào?" },
				{ "hello", "👋 Hello! Welcome to our aesthetic clinic. How can I help you today?" },
				{ "hi", "👋 Hi bạn! Rất vui được gặp bạn. Mình có thể hỗ trợ bạn về các dịch vụ thẩm mỹ, đặt lịch khám, hoặc bất cứ điều gì bạn cần!" },
				{ "chào", "👋 Chào bạn! Mình là AI Assistant. Mình sẵn sàng giúp bạn. Bạn muốn tìm hiểu về dịch vụ nào?" },

				// ===== CÂUHỎI VỀ BẢN THÂN =====
				{ "bạn là ai", "🤖 Mình là một trợ lý AI được thiết kế để hỗ trợ bạn tìm hiểu về các dịch vụ thẩm mỹ, đặt lịch khám, và trò chuyện về các vấn đề sắc đẹp." },
				{ "bạn tên gì", "👤 Mình là AI Assistant của phòng khám. Bạn có thể gọi mình là Bác sĩ AI hoặc chỉ gọi là AI!" },
				{ "ai đang nói chuyện với tôi", "🤖 Mình là AI Assistant - một trợ lý thông minh của phòng khám thẩm mỹ. Mình luôn sẵn sàng hỗ trợ bạn 24/7!" },

				// ===== CÂUHỎI VỀ KHẢNĂNG =====
				{ "bạn có thể làm gì", "✨ Mình có thể giúp bạn:\n• 🏥 Tìm kiếm dịch vụ thẩm mỹ\n• 👨‍⚕️ Xem danh sách các bác sĩ\n• 📅 Đặt lịch khám\n• ❌ Hủy lịch khám\n• 💄 Tư vấn sản phẩm chăm sóc\n• 💰 Xem giá dịch vụ\n• 💬 Trò chuyện với bạn về sắc đẹp" },
				{ "mình có thể làm gì", "✨ Bạn có thể:\n• 🔍 Tìm kiếm dịch vụ\n• 📅 Đặt/Hủy lịch hẹn\n• 💳 Thanh toán và kiểm tra hóa đơn\n• 📦 Mua sản phẩm chăm sóc\n• ❓ Hỏi bất cứ điều gì về sắc đẹp" },
				{ "bạn hỗ trợ gì", "🎯 Mình hỗ trợ:\n• Tìm dịch vụ thẩm mỹ phù hợp\n• Xem thông tin bác sĩ chuyên môn\n• Đặt lịch hẹn trực tuyến\n• Tư vấn sản phẩm & chăm sóc da\n• Quản lý lịch hẹn của bạn" },

				// ===== LỜI CẢM ƠN =====
				{ "cảm ơn", "😊 Không có gì! Mình luôn sẵn lòng giúp bạn. Nếu có bất cứ câu hỏi nào khác, đừng ngần ngại hỏi mình nhé!" },
				{ "cảm ơn bạn", "🙌 Bạn thích rồi! Mình sẵn sàng giúp bạn bất cứ lúc nào." },
				{ "thanks", "😊 You're welcome! Feel free to ask me anything." },
				{ "thank you", "😊 Glad to help! Don't hesitate to ask if you need anything else." },
				{ "tks", "😊 Vui lòng hỏi mình nếu cần thêm trợ giúp!" },

				// ===== CÂUHỎI VỀ GIỜ MỞ CỬA/LIÊN HỆ =====
				{ "mở cửa mấy giờ", "🕐 Phòng khám chúng tôi mở cửa từ 8h sáng đến 17h chiều (đóng cửa 12h-13h). Nếu cần liên hệ: 📞 0383102388" },
				{ "giờ mở cửa", "🕐 Giờ hoạt động: 08:00 - 17:00 (Nghỉ trưa 12:00-13:00). Hotline: 0383102388" },
				{ "contact", "📞 Liên hệ phòng khám:\n☎️ 0383102388 (24/7)\n📍 Phòng khám thẩm mỹ Aesthetics\n🕐 Mở cửa: 08:00-17:00 (Nghỉ 12:00-13:00)" },
				{ "hotline", "📞 Hotline hỗ trợ: **0383102388** (24/7)\n📧 Email: support@aesthetics.com\n📍 Địa chỉ: Hà Nội" },
				{ "địa chỉ", "📍 Phòng khám Aesthetics\n📌 Vị trí: Hà Nội\n☎️ Hotline: 0383102388" },

				// ===== CÂUHỎI VỀ GIỜ/NGÀY =====
				{ "bây giờ mấy giờ", $"⏰ Hiện tại là **{DateTime.Now:HH:mm}** ({DateTime.Now:dddd}, {DateTime.Now:dd/MM/yyyy})" },
				{ "hôm nay mấy giờ", $"📅 Hôm nay là **{DateTime.Now:dddd}, ngày {DateTime.Now:dd/MM/yyyy}** - **{DateTime.Now:HH:mm}**" },
				{ "hôm nay ngày mấy", $"📅 Hôm nay là **{DateTime.Now:dd/MM/yyyy}** ({DateTime.Now:dddd})" },
				{ "ngày hôm nay", $"📅 Ngày **{DateTime.Now:dd/MM/yyyy}**" },

				// ===== CÂUHỎI CHUNG VỀ SỨC KHỎE/SẮC ĐẸP =====
				{ "tôi bị mụn", "💊 Mụn là vấn đề phổ biến! Phòng khám chúng tôi có dịch vụ:\n• 🎯 Trị mụn chuyên sâu\n• 💆 Chăm sóc da mặt\n• 💄 Sản phẩm trị mụn\n\nBạn muốn tư vấn hoặc đặt lịch khám?" },
				{ "da của tôi khô", "💧 Da khô cần được cấp ẩm đúng cách!\nPhòng khám chúng tôi cung cấp:\n• 🧴 Sản phẩm dưỡng ẩm cao cấp\n• 💆 Liệu trình chăm sóc da khô\n• 👨‍⚕️ Tư vấn từ các bác sĩ\n\nBạn muốn biết thêm chi tiết?" },
				{ "da nhạy cảm", "🛡️ Da nhạy cảm cần chăm sóc đặc biệt!\nMình có thể giới thiệu:\n• Sản phẩm an toàn cho da nhạy cảm\n• Liệu trình điều trị chuyên biệt\n• Tư vấn từ bác sĩ\n\nBạn muốn liên hệ bác sĩ?" },
				{ "lão hóa", "✨ Lo lắng về dấu hiệu lão hóa?\nPhòng khám chúng tôi có:\n• 🎯 Liệu trình trẻ hóa da\n• 💉 Công nghệ chống lão hóa\n• 💄 Sản phẩm chống lão hóa\n\nBạn muốn tư vấn chi tiết?" },
				{ "tàn nhang", "🌟 Tàn nhang là vấn đề thường gặp!\nChúng tôi cung cấp:\n• 🎯 Dịch vụ trị tàn nhang\n• 💆 Chăm sóc da chuyên biệt\n• 📅 Đặt lịch khám với bác sĩ\n\nBạn muốn biết thêm?" },
				{ "mụn cơm", "🔴 Mụn cơm (blackhead) cũng có thể được trị!\nDịch vụ của chúng tôi:\n• 🧖 Làm sạch sâu lỗ chân lông\n• 💆 Chăm sóc da toàn diện\n• 💄 Sản phẩm chứa BHA/AHA\n\nBạn muốn đặt lịch?" },
				{ "nám", "🌙 Nám da là vấn đề thường gặp ở phụ nữ!\nPhòng khám cung cấp:\n• 🎯 Liệu trình trị nám chuyên sâu\n• 💡 Công nghệ laser hiện đại\n• 💄 Sản phẩm đặc trị nám\n\nHãy liên hệ để tư vấn!" },
				{ "mắt quầng", "😴 Mắt quầng làm bạn trông mệt mỏi?\nChúng tôi có giải pháp:\n• 👁️ Dịch vụ chăm sóc vùng mắt\n• 🧴 Serum & mặt nạ chuyên dụng\n• 💆 Massage thư giãn\n\nBạn muốn thử?" },
				{ "mụn viêm", "🔥 Mụn viêm cần được chăm sóc cẩn thận!\nPhòng khám chúng tôi:\n• 🎯 Trị mụn viêm an toàn\n• 💊 Sử dụng công nghệ không xâm lấn\n• 💆 Không để lại sẹo\n\nĐặt lịch tư vấn ngay!" },

				// ===== CÂUHỎI CHUNG =====
				{ "ok", "👍 Tốt! Mình sẵn sàng giúp bạn. Bạn cần gì tiếp theo?" },
				{ "được", "✅ Tuyệt vời! Bạn muốn tìm dịch vụ nào hoặc cần tư vấn gì?" },
				{ "vâng", "👍 Dạ, mình sẵn sàng!" },
				{ "không", "❌ Được rồi! Nếu cần hỗ trợ, hãy cho mình biết nhé!" },
				{ "không cần", "👍 Được thôi! Nếu sau này bạn cần gì, hãy hỏi mình. Mình luôn sẵn sàng!" },

				// ===== CÂUHỎI VỀ GIÁ =====
				{ "giá dịch vụ bao nhiêu", "💰 Giá dịch vụ thay đổi tùy theo loại dịch vụ. Bạn muốn tìm hiểu dịch vụ nào cụ thể?\n• Trị mụn\n• Trẻ hóa da\n• Chăm sóc da\n\nHãy cho mình biết để tôi báo giá chi tiết!" },
				{ "bao nhiêu tiền", "💵 Giá cả phụ thuộc vào loại dịch vụ bạn chọn. Hãy cho mình biết dịch vụ nào để mình tư vấn giá!" },
				{ "có giảm giá", "🎁 Phòng khám chúng tôi có các chương trình khuyến mãi thường xuyên!\n💳 Đặt lịch hẹn để nhận ưu đãi đặc biệt\n☎️ Hotline: 0383102388\n\nBạn muốn biết chi tiết?" },
				{ "có khuyến mãi", "🎉 Chúng tôi có nhiều khuyến mãi hấp dẫn!\n• 🎁 Ưu đãi cho khách hàng mới\n• 💳 Giảm giá dịch vụ\n• 🎯 Gói ưu đãi combo\n\nHãy liên hệ: 0383102388!" },
				{ "giá bao nhiêu", "💰 Để biết giá chính xác, bạn vui lòng:\n• Cho mình biết dịch vụ cần tư vấn\n• Hoặc liên hệ hotline: 0383102388\n\nMình sẽ báo giá chi tiết cho bạn!" },

				// ===== CÂUHỎI VỀ ĐẶT/HỦY LỊCH =====
				{ "làm sao để đặt lịch", "📅 Rất dễ! Bạn có thể:\n1️⃣ Nói cho mình biết dịch vụ muốn đặt\n2️⃣ Chọn bác sĩ và thời gian\n3️⃣ Xác nhận lịch hẹn\n\nBạn muốn đặt lịch ngay bây giờ?" },
				{ "hủy lịch", "❌ Để hủy lịch hẹn, bạn có thể:\n• Cho mình biết dịch vụ cần hủy\n• Hoặc liên hệ: 0383102388\n\nBạn muốn hủy lịch nào?" },
				{ "đặt lịch khám", "📅 Tuyệt vời! Bạn muốn đặt lịch khám dịch vụ nào?\n• 💄 Trị mụn\n• ✨ Trẻ hóa da\n• 🧴 Chăm sóc da\n• 🎯 Dịch vụ khác\n\nHãy cho mình biết!" },
				{ "muốn đặt lịch", "📅 Tuyệt vời! Bạn muốn đặt lịch khám dịch vụ nào?\n• 💄 Trị mụn\n• ✨ Trẻ hóa da\n• 🧴 Chăm sóc da\n\nHãy chọn dịch vụ để mình giúp bạn!" },
				{ "đặt lịch", "📅 Để đặt lịch, bạn vui lòng:\n1️⃣ Cho mình biết dịch vụ cần đặt\n2️⃣ Chọn ngày và giờ phù hợp\n3️⃣ Chọn bác sĩ (nếu cần)\n\nBắt đầu nào!" },

				// ===== CÂUHỎI VỀ SẢN PHẨM =====
				{ "có sản phẩm nào", "📦 Phòng khám chúng tôi cung cấp sản phẩm chăm sóc da cao cấp:\n• 🧴 Nước hoa hồng & toner\n• 💧 Serum & essence\n• 🧴 Kem dưỡng\n• 🧖 Mặt nạ\n\nBạn muốn tìm hiểu sản phẩm nào?" },
				{ "sản phẩm", "📦 Chúng tôi có nhiều sản phẩm chăm sóc da!\nBạn muốn tìm sản phẩm về:\n• 🎯 Trị mụn\n• 💧 Dưỡng ẩm\n• ✨ Chống lão hóa\n• 🌙 Chăm sóc đêm\n\nBạn cần gì?" },
				{ "có bác sĩ nào giỏi", "👨‍⚕️ Phòng khám chúng tôi có đội ngũ bác sĩ chuyên môn cao!\nBạn muốn:\n• 📋 Xem danh sách bác sĩ\n• 👤 Tìm bác sĩ chuyên khoa nào đó\n• 📅 Đặt lịch với bác sĩ cụ thể\n\nHãy cho mình biết!" },

				// ===== PHẢN HỒI TÍCH CỰC =====
				{ "tuyệt vời", "🎉 Tuyệt vời! Mình rất vui lòng! Bạn muốn biết thêm gì nữa không?" },
				{ "hay", "👍 Cảm ơn bạn! Mình sẽ cố gắng giúp bạn tốt nhất!" },
				{ "tốt", "✅ Tốt! Bạn muốn tìm hiểu gì tiếp theo?" },
				{ "quá tốt", "🌟 Cảm ơn bạn rất nhiều! Hãy liên hệ mình nếu cần thêm trợ giúp!" },

				// ===== PHẢN HỒI TIÊU CỰC =====
				{ "không tốt", "😔 Mình rất xin lỗi! Bạn có ý kiến gì để mình cải thiện? Liên hệ: 0383102388" },
				{ "tệ", "😟 Mình xin lỗi! Vui lòng liên hệ hotline để được hỗ trợ tốt hơn: 0383102388" },
				{ "chán", "😞 Mình hiểu! Bạn muốn tìm hiểu dịch vụ hay sản phẩm gì khác không?" },

				// ===== CÂU HỎI VỀ ĐỘ TIN CẬY =====
				{ "tin cậy", "✅ Phòng khám chúng tôi là địa chỉ uy tín!\n• 👨‍⚕️ Bác sĩ chuyên môn cao\n• 🏥 Cơ sở vật chất hiện đại\n• 😊 Khách hàng hài lòng\n\nBạn yên tâm 100%!" },
				{ "an toàn", "🛡️ Dịch vụ của chúng tôi 100% an toàn!\n• ✓ Tiệt trùng đầy đủ\n• ✓ Dụng cụ y tế chuẩn\n• ✓ Bác sĩ có giấy phép\n\nBạn có thể yên tâm!" },
				{ "phòng khám như thế nào", "🏥 Phòng khám chúng tôi:\n• 🌟 Hiện đại & sạch sẽ\n• 👨‍⚕️ Bác sĩ giàu kinh nghiệm\n• 😊 Dịch vụ tận tâm\n• 💰 Giá cả hợp lý\n\nHãy ghé thăm!" },
				{ "có ai không", "👋 Có! Bạn có thể:\n• 💬 Chat với mình (AI)\n• ☎️ Gọi hotline: 0383102388\n• 👨‍⚕️ Tư vấn trực tiếp với bác sĩ\n\nChọn cách nào?" },
				{ "bạn giúp gì được", "🎯 Mình giúp bạn:\n• 📝 Tư vấn dịch vụ\n• 📅 Đặt/Hủy lịch\n• 💰 Thông tin giá\n• ❓ Trả lời câu hỏi\n• 📱 Hướng dẫn sử dụng\n\nHỏi mình bất cứ gì!" },
			};

			// 🔥 ENHANCED MATCHING LOGIC WITH TOOL PRIORITY:
			// 1️⃣ Check if query is about services/doctors - SKIP friendly responses
			var toolKeywords = new[] { "bác sĩ", "doctor", "dịch vụ", "service", "liệu trình", "treatment", "buổi", "session", "appointment", "lịch", "đặt", "hủy", "giá", "price", "sản phẩm", "product", "giỏ hàng", "cart", "top", "phổ biến", "popular" };
			if (toolKeywords.Any(kw => lowerQuery.Contains(kw)))
			{
				_logger.LogInformation("[SKIP_FRIENDLY] Query contains tool keywords - returning null to proceed with LLM");
				return null; // Bỏ qua friendly responses, cho phép LLM xử lý
			}

			// 2️⃣ Exact match for true friendly questions
			if (friendlyResponses.TryGetValue(lowerQuery, out var exactMatch))
			{
				_logger.LogInformation("[FRIENDLY_MATCH] Exact match: {Key}", lowerQuery);
				return ReturnFriendlyResponse(exactMatch);
			}

			// 3️⃣ Word-based matching (whole word, not substring)
			var words = lowerQuery.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var kvp in friendlyResponses)
			{
				// Kiểm tra xem query có chứa key như một từ hoàn chỉnh không
				var keyWords = kvp.Key.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				// Nếu query chính là một word hoàn chỉnh khớp với key
				if (words.Length == 1 && words[0] == kvp.Key)
				{
					_logger.LogInformation("[FRIENDLY_MATCH] Single word exact match: {Key}", kvp.Key);
					return ReturnFriendlyResponse(kvp.Value);
				}

				// Nếu tất cả từ của key đều có trong query (word-based)
				if (keyWords.All(keyWord => words.Any(w => w == keyWord)))
				{
					_logger.LogInformation("[FRIENDLY_MATCH] Word-based match: {Key}", kvp.Key);
					return ReturnFriendlyResponse(kvp.Value);
				}
			}

			// 4️⃣ Contains matching (last resort - chỉ khi không có match nào khác)
			// ⚠️ TỰ ĐỘNG LOẠI BỎ những key quá ngắn (1-2 ký tự) để tránh false positive
			foreach (var kvp in friendlyResponses.Where(x => x.Key.Length > 2))
			{
				if (lowerQuery.Contains(kvp.Key))
				{
					_logger.LogInformation("[FRIENDLY_MATCH] Contains match: {Key}", kvp.Key);
					return ReturnFriendlyResponse(kvp.Value);
				}
			}

			return null; // Không phải friendly question
		}

		/// <summary>
		/// Helper method để tạo friendly response
		/// </summary>
		private dynamic ReturnFriendlyResponse(string message)
		{
			dynamic response = new System.Dynamic.ExpandoObject();
			response.success = true;
			response.message = message;
			response.data = null;
			response.toolUsed = "chatbot_friendly";
			response.conversationUpdate = new
			{
				role = "assistant",
				content = message
			};

			return response;
		}
		#endregion
	}
}