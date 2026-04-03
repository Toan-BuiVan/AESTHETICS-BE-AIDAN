using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.RequestModel.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices.AI
{
	public class AIFunctionCallingService : IAIFunctionCallingService
	{
		private readonly ILogger<AIFunctionCallingService> _logger;
		private readonly IAppointmentRepositoty _appointmentRepository;
		private readonly IServiceRepository _serviceRepository;
		private readonly IStaffRepository _staffRepository;
		private readonly IProductRepository _productRepository;
		private readonly IInvoiceDetailsRepository _invoiceDetailsRepository;
		private readonly ICustomerRepository _customerRepository;

		public AIFunctionCallingService(
			ILogger<AIFunctionCallingService> logger,
			IAppointmentRepositoty appointmentRepository,
			IServiceRepository serviceRepository,
			IStaffRepository staffRepository,
			IProductRepository productRepository,
			IInvoiceDetailsRepository invoiceDetailsRepository,
			ICustomerRepository customerRepository)
		{
			_logger = logger;
			_appointmentRepository = appointmentRepository;
			_serviceRepository = serviceRepository;
			_staffRepository = staffRepository;
			_productRepository = productRepository;
			_invoiceDetailsRepository = invoiceDetailsRepository;
			_customerRepository = customerRepository;
		}

		public async Task<AIToolsListResponse> GetAvailableToolsAsync()
		{
			try
			{
				_logger.LogInformation("Getting available tools for AI Function Calling");

				var tools = new List<AITool>
				{
					new AITool
					{
						Name = "getServiceAvailableSlots",
						Description = "Lấy danh sách ngày trống của một dịch vụ trong khoảng thời gian",
						InputSchema = new Dictionary<string, string>
						{
							{ "serviceId", "int - ID của dịch vụ" },
							{ "startDate", "string (YYYY-MM-DD) - Ngày bắt đầu" },
							{ "endDate", "string (YYYY-MM-DD) - Ngày kết thúc" },
							{ "staffId", "int? (optional) - ID nhân viên cụ thể" }
						},
						OutputDescription = "Danh sách các slot trống: [{ date, time, staffId, staffName }]",
						Example = "serviceId: 1, startDate: 2026-03-31, endDate: 2026-04-06"
					},

					new AITool
					{
						Name = "getDoctorAvailableSlots",
						Description = "Lấy lịch trống của một bác sĩ cụ thể trong ngày",
						InputSchema = new Dictionary<string, string>
						{
							{ "staffId", "int - ID của bác sĩ" },
							{ "date", "string (YYYY-MM-DD) - Ngày cần kiểm tra" }
						},
						OutputDescription = "Danh sách giờ trống: [{ time, available, appointmentCount }]",
						Example = "staffId: 5, date: 2026-03-31"
					},

					new AITool
					{
						Name = "getTopSellingProducts",
						Description = "Lấy danh sách sản phẩm bán chạy nhất",
						InputSchema = new Dictionary<string, string>
						{
							{ "limit", "int (default: 10) - Số lượng sản phẩm cần lấy" }
						},
						OutputDescription = "Danh sách sản phẩm: [{ productId, name, sellingPrice, serviceType, soldCount }]",
						Example = "limit: 5"
					},

					new AITool
					{
						Name = "bookAppointment",
						Description = "Đặt một lịch hẹn mới",
						InputSchema = new Dictionary<string, string>
						{
							{ "customerId", "int - ID khách hàng" },
							{ "serviceId", "int - ID dịch vụ" },
							{ "staffId", "int - ID nhân viên/bác sĩ" },
							{ "appointmentDate", "string (YYYY-MM-DD) - Ngày hẹn" },
							{ "appointmentTime", "string (HH:mm) - Giờ hẹn" },
							{ "notes", "string? (optional) - Ghi chú" }
						},
						OutputDescription = "Thông tin lịch hẹn: { appointmentId, confirmationCode, status }",
						Example = "customerId: 1, serviceId: 1, staffId: 5, appointmentDate: 2026-03-31, appointmentTime: 08:00"
					},

					new AITool
					{
						Name = "searchProducts",
						Description = "Tìm kiếm sản phẩm theo tên hoặc danh mục",
						InputSchema = new Dictionary<string, string>
						{
							{ "keyword", "string? - Từ khóa tìm kiếm" },
							{ "minPrice", "decimal? - Giá tối thiểu" },
							{ "maxPrice", "decimal? - Giá tối đa" },
							{ "limit", "int (default: 10)" }
						},
						OutputDescription = "Danh sách sản phẩm phù hợp",
						Example = "keyword: 'dưỡng da', limit: 5"
					},

					new AITool
					{
						Name = "searchDoctors",
						Description = "Tìm kiếm bác sĩ theo chuyên khoa",
						InputSchema = new Dictionary<string, string>
						{
							{ "specialization", "string? - Chuyên khoa (Da liễu, Phẫu thuật thẩm mỹ, ...)" },
							{ "isAvailable", "bool? - Chỉ lấy bác sĩ đang có lịch trống" }
						},
						OutputDescription = "Danh sách bác sĩ: [{ staffId, name, specialization, experience, rating }]",
						Example = "specialization: 'Da liễu', isAvailable: true"
					}
				};

				var response = new AIToolsListResponse
				{
					SystemPrompt = @"Bạn là trợ lý AI thông minh cho hệ thống đặt lịch spa và bán hàng. 
Nhiệm vụ của bạn là:
1. Phân tích yêu cầu của người dùng
2. Quyết định sử dụng tool (API) nào phù hợp
3. Trả về response theo format JSON được yêu cầu

Ghi chú:
- Nếu người dùng muốn kiểm tra lịch trống = dùng getServiceAvailableSlots hoặc getDoctorAvailableSlots
- Nếu người dùng muốn đặt lịch = dùng bookAppointment (nhưng trước đó phải kiểm tra lịch trống)
- Nếu người dùng muốn xem sản phẩm = dùng getTopSellingProducts hoặc searchProducts
- Nếu người dùng muốn tìm bác sĩ = dùng searchDoctors
- Luôn trả về JSON hợp lệ",

					Tools = tools,

					ResponseFormat = @"{
  ""tool"": ""toolName"",
  ""params"": {
    ""param1"": ""value1"",
    ""param2"": ""value2""
  },
  ""reasoning"": ""Giải thích tại sao chọn tool này (optional)""
}"
				};

				_logger.LogInformation("Retrieved {Count} available tools", tools.Count);
				return response;
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
					"getServiceAvailableSlots" => await GetServiceAvailableSlotsAsync(request.LLMResponse.Params),
					"getDoctorAvailableSlots" => await GetDoctorAvailableSlotsAsync(request.LLMResponse.Params),
					"getTopSellingProducts" => await GetTopSellingProductsAsync(request.LLMResponse.Params),
					"bookAppointment" => await BookAppointmentAsync(request.LLMResponse.Params, request.UserId),
					"searchProducts" => await SearchProductsAsync(request.LLMResponse.Params),
					"searchDoctors" => await SearchDoctorsAsync(request.LLMResponse.Params),
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
					"getServiceAvailableSlots" => ValidateServiceSlotsParams(@params),
					"getDoctorAvailableSlots" => ValidateDoctorSlotsParams(@params),
					"getTopSellingProducts" => ValidateProductParams(@params),
					"bookAppointment" => ValidateBookingParams(@params),
					"searchProducts" => ValidateSearchProductParams(@params),
					"searchDoctors" => ValidateSearchDoctorParams(@params),
					_ => false
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Validation error for tool {Tool}", toolName);
				return false;
			}
		}

		// ✅ IMPLEMENTATION: Get Service Available Slots
		private async Task<AIExecuteToolResponse> GetServiceAvailableSlotsAsync(Dictionary<string, object> @params)
		{
			try
			{
				if (!int.TryParse(@params["serviceId"].ToString(), out var serviceId))
					return new AIExecuteToolResponse { Success = false, Error = "Invalid serviceId" };

				if (!DateTime.TryParse(@params["startDate"].ToString(), out var startDate))
					return new AIExecuteToolResponse { Success = false, Error = "Invalid startDate" };

				if (!DateTime.TryParse(@params["endDate"].ToString(), out var endDate))
					return new AIExecuteToolResponse { Success = false, Error = "Invalid endDate" };

				int? staffId = null;
				if (@params.ContainsKey("staffId") && int.TryParse(@params["staffId"]?.ToString() ?? "", out var sId))
					staffId = sId;

				_logger.LogInformation("Getting available slots for service {ServiceId} from {StartDate} to {EndDate}",
					serviceId, startDate, endDate);

				// ✅ Kiểm tra service tồn tại
				var service = await _serviceRepository.GetById(serviceId);
				if (service == null)
					return new AIExecuteToolResponse { Success = false, Error = "Service not found" };

				// ✅ Lấy tất cả appointments của dịch vụ trong khoảng thời gian
				var appointments = await _appointmentRepository
					.FindByPredicate(x => x.ServiceId == serviceId &&
										 x.StartTime >= startDate &&
										 x.StartTime <= endDate.AddDays(1) &&
										 !x.DeleteStatus);

				// ✅ Lấy tất cả staff có thể làm dịch vụ này
				var allStaff = await _staffRepository.FindByPredicate(x => !x.DeleteStatus);

				// Nếu có staffId cụ thể thì lọc
				if (staffId.HasValue)
					allStaff = allStaff.Where(x => x.Id == staffId.Value).ToList();

				// ✅ Tính toán slots trống
				var slots = new List<dynamic>();
				var currentDate = startDate.Date;

				while (currentDate <= endDate.Date)
				{
					// Bỏ qua ngày Chủ Nhật
					if (currentDate.DayOfWeek == DayOfWeek.Sunday)
					{
						currentDate = currentDate.AddDays(1);
						continue;
					}

					// Giờ làm việc: 8:00 - 17:00, mỗi slot 1 giờ
					for (int hour = 8; hour < 17; hour++)
					{
						var slotTime = currentDate.AddHours(hour);

						// Lấy các appointments trong giờ này
						var appointmentsInSlot = appointments
							.Where(a => a.StartTime.HasValue &&
									   a.StartTime.Value.Date == currentDate.Date &&
									   a.StartTime.Value.Hour == hour)
							.ToList();

						// Tìm staff có sẵn trong giờ này
						foreach (var staff in allStaff)
						{
							var staffHasAppointment = appointmentsInSlot.Any(a => a.StaffId == staff.Id);

							if (!staffHasAppointment)
							{
								slots.Add(new
								{
									date = currentDate.ToString("yyyy-MM-dd"),
									time = $"{hour:D2}:00",
									staffId = staff.Id,
									staffName = staff.FullName ?? "N/A"
								});
							}
						}
					}

					currentDate = currentDate.AddDays(1);
				}

				_logger.LogInformation("Found {Count} available slots for service {ServiceId}", slots.Count, serviceId);

				return new AIExecuteToolResponse
				{
					Success = true,
					Data = slots,
					Message = $"Found {slots.Count} available slots"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting service available slots");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		// ✅ IMPLEMENTATION: Get Doctor Available Slots
		private async Task<AIExecuteToolResponse> GetDoctorAvailableSlotsAsync(Dictionary<string, object> @params)
		{
			try
			{
				if (!int.TryParse(@params["staffId"].ToString(), out var staffId))
					return new AIExecuteToolResponse { Success = false, Error = "Invalid staffId" };

				if (!DateTime.TryParse(@params["date"].ToString(), out var date))
					return new AIExecuteToolResponse { Success = false, Error = "Invalid date" };

				_logger.LogInformation("Getting available slots for doctor {StaffId} on {Date}",
					staffId, date);

				// ✅ Kiểm tra doctor/staff tồn tại
				var staff = await _staffRepository.GetById(staffId);
				if (staff == null)
					return new AIExecuteToolResponse { Success = false, Error = "Doctor not found" };

				// ✅ Lấy tất cả appointments của bác sĩ trong ngày đó
				var appointmentsOnDate = await _appointmentRepository
					.FindByPredicate(x => x.StaffId == staffId &&
										 x.StartTime.HasValue &&
										 x.StartTime.Value.Date == date.Date &&
										 !x.DeleteStatus);

				// ✅ Tính toán slots trống (8:00 - 17:00)
				var slots = new List<dynamic>();

				for (int hour = 8; hour < 17; hour++)
				{
					var appointmentsInSlot = appointmentsOnDate
						.Where(a => a.StartTime.HasValue && a.StartTime.Value.Hour == hour)
						.ToList();

					slots.Add(new
					{
						time = $"{hour:D2}:00",
						available = appointmentsInSlot.Count == 0,
						appointmentCount = appointmentsInSlot.Count
					});
				}

				_logger.LogInformation("Found {Count} slots for doctor {StaffId} on {Date}",
					slots.Count, staffId, date);

				return new AIExecuteToolResponse
				{
					Success = true,
					Data = slots,
					Message = $"Found {slots.Count(s => (bool)s.available)} available slots"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting doctor available slots");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		// ✅ IMPLEMENTATION: Get Top Selling Products
		private async Task<AIExecuteToolResponse> GetTopSellingProductsAsync(Dictionary<string, object> @params)
		{
			try
			{
				int limit = @params.ContainsKey("limit") && int.TryParse(@params["limit"].ToString(), out var l) ? l : 10;

				_logger.LogInformation("Getting top {Limit} selling products", limit);

				// ✅ Lấy tất cả sản phẩm
				var allProducts = await _productRepository.FindByPredicate(x => !x.DeleteStatus);

				// ✅ Lấy thống kê bán hàng từ InvoiceDetail
				var invoiceDetails = await _invoiceDetailsRepository
					.FindByPredicate(x => !x.DeleteStatus);

				// ✅ Group by ProductId để lấy tổng số lượng bán
				var productSalesMap = invoiceDetails
					.GroupBy(x => x.ProductId)
					.ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

				// ✅ Join và sắp xếp theo số lượng bán
				var topProducts = allProducts
					.Select(p => new
					{
						productId = p.Id,
						name = p.ProductName ?? "N/A",
						sellingPrice = p.SellingPrice ?? 0,
						serviceType = p.ServiceType?.ServiceTypeName ?? "N/A",
						soldCount = productSalesMap.ContainsKey(p.Id) ? productSalesMap[p.Id] : 0,
						quantity = p.Quantity
					})
					.OrderByDescending(x => x.soldCount)
					.Take(limit)
					.ToList();

				_logger.LogInformation("Found {Count} top selling products", topProducts.Count);

				return new AIExecuteToolResponse
				{
					Success = true,
					Data = topProducts,
					Message = $"Found {topProducts.Count} top selling products"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting top selling products");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		// ✅ IMPLEMENTATION: Book Appointment
		private async Task<AIExecuteToolResponse> BookAppointmentAsync(Dictionary<string, object> @params, int userId)
		{
			try
			{
				if (!int.TryParse(@params["customerId"].ToString(), out var customerId))
					return new AIExecuteToolResponse { Success = false, Error = "Invalid customerId" };

				if (!int.TryParse(@params["serviceId"].ToString(), out var serviceId))
					return new AIExecuteToolResponse { Success = false, Error = "Invalid serviceId" };

				if (!int.TryParse(@params["staffId"].ToString(), out var staffId))
					return new AIExecuteToolResponse { Success = false, Error = "Invalid staffId" };

				if (!DateTime.TryParse(@params["appointmentDate"].ToString(), out var appointmentDate))
					return new AIExecuteToolResponse { Success = false, Error = "Invalid appointmentDate" };

				var appointmentTimeStr = @params["appointmentTime"].ToString();
				if (!DateTime.TryParse($"{appointmentDate:yyyy-MM-dd} {appointmentTimeStr}", out var appointmentDateTime))
					return new AIExecuteToolResponse { Success = false, Error = "Invalid appointmentTime" };

				var notes = @params.ContainsKey("notes") ? @params["notes"]?.ToString() : null;

				_logger.LogInformation("Booking appointment for customer {CustomerId} with service {ServiceId} at {DateTime}",
					customerId, serviceId, appointmentDateTime);

				// ✅ Validate customer, service, staff exist
				var customer = await _customerRepository.GetById(customerId);
				if (customer == null)
					return new AIExecuteToolResponse { Success = false, Error = "Customer not found" };

				var service = await _serviceRepository.GetById(serviceId);
				if (service == null)
					return new AIExecuteToolResponse { Success = false, Error = "Service not found" };

				var staff = await _staffRepository.GetById(staffId);
				if (staff == null)
					return new AIExecuteToolResponse { Success = false, Error = "Staff not found" };

				// ✅ Check if slot is already booked
				var existingAppointment = await _appointmentRepository
					.FindByPredicate(x => x.ServiceId == serviceId &&
										 x.StaffId == staffId &&
										 x.StartTime == appointmentDateTime &&
										 !x.DeleteStatus);

				if (existingAppointment.Any())
					return new AIExecuteToolResponse { Success = false, Error = "Slot already booked" };

				// ✅ Create new appointment
				var appointment = new AppointmentEntity
				{
					CustomerId = customerId,
					ServiceId = serviceId,
					StaffId = staffId,
					StartTime = appointmentDateTime,
					CreationDate = DateTime.Now,
					Status = 0, // DaDat
					PaymentStatus = 0,
					DeleteStatus = false
				};

				var created = await _appointmentRepository.CreateEntity(appointment);
				if (!created)
					return new AIExecuteToolResponse { Success = false, Error = "Failed to create appointment" };

				var confirmationCode = $"APP-{appointment.Id:D6}";

				_logger.LogInformation("Appointment booked successfully: {ConfirmationCode}", confirmationCode);

				return new AIExecuteToolResponse
				{
					Success = true,
					Data = new { appointmentId = appointment.Id, confirmationCode, status = "Confirmed" },
					Message = $"Appointment booked successfully with code: {confirmationCode}"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error booking appointment");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		// ✅ IMPLEMENTATION: Search Products
		private async Task<AIExecuteToolResponse> SearchProductsAsync(Dictionary<string, object> @params)
		{
			try
			{
				var keyword = @params.ContainsKey("keyword") ? @params["keyword"]?.ToString() : null;

				decimal minPrice = @params.ContainsKey("minPrice") && decimal.TryParse(@params["minPrice"]?.ToString() ?? "", out var minP) ? minP : 0;
				decimal maxPrice = @params.ContainsKey("maxPrice") && decimal.TryParse(@params["maxPrice"]?.ToString() ?? "", out var maxP) ? maxP : decimal.MaxValue;

				int limit = @params.ContainsKey("limit") && int.TryParse(@params["limit"].ToString(), out var l) ? l : 10;

				_logger.LogInformation("Searching products with keyword: {Keyword}, minPrice: {MinPrice}, maxPrice: {MaxPrice}",
					keyword, minPrice, maxPrice);

				// ✅ Query products
				var products = await _productRepository.FindByPredicate(x => !x.DeleteStatus);

				// ✅ Filter by keyword
				if (!string.IsNullOrWhiteSpace(keyword))
				{
					products = products.Where(p =>
						(p.ProductName ?? "").ToLower().Contains(keyword.ToLower()) ||
						(p.Description ?? "").ToLower().Contains(keyword.ToLower())
					).ToList();
				}

				// ✅ Filter by price range
				products = products.Where(p =>
					(p.SellingPrice ?? 0) >= minPrice &&
					(p.SellingPrice ?? 0) <= maxPrice
				).ToList();

				// ✅ Map to response
				var result = products
					.Take(limit)
					.Select(p => new
					{
						productId = p.Id,
						name = p.ProductName ?? "N/A",
						sellingPrice = p.SellingPrice ?? 0,
						serviceType = p.ServiceType?.ServiceTypeName ?? "N/A",
						quantity = p.Quantity,
						description = p.Description
					})
					.ToList();

				_logger.LogInformation("Found {Count} products matching search criteria", result.Count);

				return new AIExecuteToolResponse
				{
					Success = true,
					Data = result,
					Message = $"Found {result.Count} products"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error searching products");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		// ✅ IMPLEMENTATION: Search Doctors
		private async Task<AIExecuteToolResponse> SearchDoctorsAsync(Dictionary<string, object> @params)
		{
			try
			{
				var specialization = @params.ContainsKey("specialization") ? @params["specialization"]?.ToString() : null;
				bool? isAvailable = @params.ContainsKey("isAvailable") && bool.TryParse(@params["isAvailable"].ToString(), out var ia) ? (bool?)ia : null;

				_logger.LogInformation("Searching doctors with specialization: {Specialization}, isAvailable: {IsAvailable}",
					specialization, isAvailable);

				// ✅ Get all doctors (IsDoctor = true)
				var doctors = await _staffRepository.FindByPredicate(x =>
					x.IsDoctor == true &&
					!x.DeleteStatus);

				// ✅ Filter by specialization
				if (!string.IsNullOrWhiteSpace(specialization))
				{
					doctors = doctors.Where(d =>
						(d.Specialization ?? "").ToLower().Contains(specialization.ToLower())
					).ToList();
				}

				// ✅ Filter by availability if requested
				if (isAvailable.HasValue && isAvailable.Value)
				{
					// Lấy doctors có appointments trong hôm nay
					var todayAppointments = await _appointmentRepository
						.FindByPredicate(x =>
							x.StartTime.HasValue &&
							x.StartTime.Value.Date == DateTime.Now.Date &&
							!x.DeleteStatus);

					var busyStaffIds = todayAppointments.Select(a => a.StaffId).Where(s => s.HasValue).Select(s => s.Value).Distinct().ToList();

					doctors = doctors.Where(d => !busyStaffIds.Contains(d.Id)).ToList();
				}

				// ✅ Map to response
				var result = doctors
					.Select(d => new
					{
						staffId = d.Id,
						name = d.FullName ?? "N/A",
						specialization = d.Specialization ?? "N/A",
						experience = d.ExperienceYears.HasValue ? d.ExperienceYears.Value : 0,
						degree = d.Degree ?? "N/A",
						isDoctor = d.IsDoctor
					})
					.ToList();

				_logger.LogInformation("Found {Count} doctors matching criteria", result.Count);

				return new AIExecuteToolResponse
				{
					Success = true,
					Data = result,
					Message = $"Found {result.Count} doctors"
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error searching doctors");
				return new AIExecuteToolResponse { Success = false, Error = ex.Message };
			}
		}

		// ✅ Validation methods
		private bool ValidateServiceSlotsParams(Dictionary<string, object> @params)
		{
			return @params.ContainsKey("serviceId") &&
				   @params.ContainsKey("startDate") &&
				   @params.ContainsKey("endDate");
		}

		private bool ValidateDoctorSlotsParams(Dictionary<string, object> @params)
		{
			return @params.ContainsKey("staffId") &&
				   @params.ContainsKey("date");
		}

		private bool ValidateProductParams(Dictionary<string, object> @params)
		{
			return true;
		}

		private bool ValidateBookingParams(Dictionary<string, object> @params)
		{
			return @params.ContainsKey("customerId") &&
				   @params.ContainsKey("serviceId") &&
				   @params.ContainsKey("staffId") &&
				   @params.ContainsKey("appointmentDate") &&
				   @params.ContainsKey("appointmentTime");
		}

		private bool ValidateSearchProductParams(Dictionary<string, object> @params)
		{
			return true;
		}

		private bool ValidateSearchDoctorParams(Dictionary<string, object> @params)
		{
			return true;
		}
	}
}