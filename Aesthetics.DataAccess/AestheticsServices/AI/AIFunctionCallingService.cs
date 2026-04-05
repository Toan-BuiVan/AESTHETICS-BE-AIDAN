using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.AI;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.RequestModel.AI;
using Aesthetics.Entities.Models.ResponseModel.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

		public AIFunctionCallingService(
			ILogger<AIFunctionCallingService> logger,
			IAIAppointmentService aiAppointmentService,
			IAIAnalyticsService aiAnalyticsService,
			IAICartService aiCartService,
			ILLMService llmService,
			IStaffRepository staffRepository,
			IServiceRepository serviceRepository,
			ITreatmentPlanRepository treatmentPlanRepository)  
		{
			_logger = logger;
			_aiAppointmentService = aiAppointmentService;
			_aiAnalyticsService = aiAnalyticsService;
			_aiCartService = aiCartService;
			_llmService = llmService;
			_staffRepository = staffRepository;
			_serviceRepository = serviceRepository;
			_treatmentPlanRepository = treatmentPlanRepository;  
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
				var customerId = Convert.ToInt32(@params["customerId"]);
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

		private async Task<AIExecuteToolResponse> ExecuteCancelAppointment(Dictionary<string, object> @params, int userId)
		{
			try
			{
				var customerId = Convert.ToInt32(@params["customerId"]);
				var staffId = Convert.ToInt32(@params["staffId"]);
				
				DateTime? appointmentDate = null;
				if (@params.ContainsKey("appointmentDate") && @params["appointmentDate"] != null)
				{
					var dateStr = @params["appointmentDate"].ToString();
					if (DateTime.TryParse(dateStr, out var date))
					{
						appointmentDate = date;
					}
				}

				int? serviceId = null;
				if (@params.ContainsKey("serviceId") && @params["serviceId"] != null)
				{
					serviceId = Convert.ToInt32(@params["serviceId"]);
				}

				var result = await _aiAppointmentService.CancelAppointmentAsync(customerId, staffId, appointmentDate, serviceId);
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
				_logger.LogError(ex, "Error in ExecuteCancelAppointment");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
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

		// ===== VALIDATION METHODS =====
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
			=> @params.ContainsKey("customerId") && @params.ContainsKey("staffId");

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

				// ✅ STEP 1: Lấy tools có sẵn
				var toolsList = await GetAvailableToolsAsync();
				_logger.LogInformation("[STEP 2] Retrieved {ToolCount} tools", toolsList.Tools.Count);

				// ✅ STEP 2: Build system prompt
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

				if (llmResponse == null)
				{
					return new
					{
						success = false,
						message = "LLM did not return a valid response",
						userMessage = "Xin lỗi, tôi không thể hiểu yêu cầu của bạn. Vui lòng thử lại."
					};
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

				// ✅ STEP 5: Validate params
				bool isValidParams = await ValidateToolParamsAsync(toolName, toolParams);
				if (!isValidParams)
				{
					return new
					{
						success = false,
						message = $"Invalid parameters for tool: {toolName}",
						userMessage = "Xin lỗi, tôi không hiểu yêu cầu của bạn. Vui lòng cung cấp thông tin đầy đủ."
					};
				}

				// ✅ STEP 6: Execute tool
				var executeRequest = new AIExecuteToolRequest
				{
					UserId = request.UserId,
					LLMResponse = llmResponse
				};

				var toolResult = await ExecuteToolAsync(executeRequest);
				_logger.LogInformation("[STEP 7] Tool executed successfully - Success: {Success}", toolResult.Success);

				// ✅ STEP 7: Format final response
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
				return new
				{
					success = false,
					message = "Error processing your request",
					error = ex.Message,
					userMessage = "Xin lỗi, có lỗi xảy ra. Vui lòng thử lại sau."
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

				10. getProductDetail - Chi tiết sản phẩm
					Params: {""productId"": int}

				11. getProductsByPriceRange - Sản phẩm theo khoảng giá
					Params: {""minPrice"": decimal, ""maxPrice"": decimal}

				12. addProductToCart - Thêm sản phẩm vào giỏ hàng
					Params: {""productId"": int, ""quantity"": int}

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
					- Ngày ""08-04-2026"" → Đổi thành ""2026-04-08"" (YYYY-MM-DD)
					- Ngày ""15/4"" → Đổi thành ""2026-04-15""
					- Giờ ""8h30 sáng"" → Đổi thành ""08:30"" (HH:mm)
					- Giờ ""14h"" → Đổi thành ""14:00"" (HH:mm)
					- Giờ ""3h chiều"" → Đổi thành ""15:00"" (HH:mm - chuyển 24h)
					- Giờ ""8:30"" → Đổi thành ""08:30"" (HH:mm)

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

				Current Date: " + DateTime.UtcNow.ToString("yyyy-MM-dd") + @"
				Current Time: " + DateTime.UtcNow.ToString("HH:mm:ss");
		}

		private List<LLMMessage> BuildConversationMessages(
			string systemPrompt,
			string userQuery,
			List<AIConversationMessage> history)
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
	}
}