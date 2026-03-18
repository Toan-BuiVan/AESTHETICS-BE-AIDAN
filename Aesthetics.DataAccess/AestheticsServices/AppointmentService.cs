using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.EmailService;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Enum;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
	public class AppointmentService : IAppointmentService
	{
		private readonly ILogger<AppointmentService> _logger;
		private readonly IAppointmentRepositoty _appointmentRepositoty;
		private readonly IAppointmentAssignmentRepository _appointmentAssignmentRepository;
		private readonly IServiceRepository _serviceRepository;
		private readonly IClinicStaffRepository _clinicStaffRepository;
		private readonly IServiceTypeRepository _serviceTypeRepository;
		private readonly IAppointmentTimeLockRepository _appointmentTimeLockRepository;
		private readonly ITreatmentPlanRepository _treatmentPlanRepository;
		private readonly ICustomerTreatmentPlansRepository _customerTreatmentPlansRepository;
		private readonly IEmailService _emailService;
		private readonly ICustomerRepository _customerRepository;
		private readonly IStaffRepository _staffRepository;
		private readonly IInvoiceRepository _invoiceRepository;
		private readonly IInvoiceDetailsRepository _invoiceDetailsRepository;
		private readonly IPerformanceLogRepository _performanceLogRepository;
		private readonly IVoucherRepository _voucherRepository;
		private readonly IWalletRepository _walletRepository;

		// Constants for better maintainability
		private const int MAX_DOCTOR_DAILY_LIMIT = 10;  // Giới hạn bác sĩ
		private const int MAX_CLINIC_DAILY_LIMIT = 50;  // Giới hạn phòng khám  
		private const int DEFAULT_REMINDER_HOURS = 24;  // Nhắc nhở trước 24h

		public AppointmentService(ILogger<AppointmentService> logger,
			IAppointmentRepositoty appointmentRepositoty,
			IAppointmentAssignmentRepository appointmentAssignmentRepository,
			IServiceRepository serviceRepository,
			IClinicStaffRepository clinicStaffRepository,
			IServiceTypeRepository serviceTypeRepository,
			IAppointmentTimeLockRepository appointmentTimeLockRepository,
			ITreatmentPlanRepository treatmentPlanRepository,
			ICustomerTreatmentPlansRepository customerTreatmentPlansRepository,
			IEmailService emailService,
			ICustomerRepository customerRepository,
			IStaffRepository staffRepository, 
			IInvoiceRepository invoiceRepository,
			IInvoiceDetailsRepository invoiceDetailsRepository,
			IPerformanceLogRepository performanceLogRepository,
			IVoucherRepository voucherRepository,
			IWalletRepository walletRepository)
		{
			_logger = logger;
			_appointmentRepositoty = appointmentRepositoty;
			_appointmentAssignmentRepository = appointmentAssignmentRepository;
			_serviceRepository = serviceRepository;
			_clinicStaffRepository = clinicStaffRepository;
			_serviceTypeRepository = serviceTypeRepository;
			_appointmentTimeLockRepository = appointmentTimeLockRepository;
			_treatmentPlanRepository = treatmentPlanRepository;
			_customerTreatmentPlansRepository = customerTreatmentPlansRepository;
			_emailService = emailService;
			_customerRepository = customerRepository;
			_staffRepository = staffRepository;
			_invoiceRepository = invoiceRepository;
			_invoiceDetailsRepository = invoiceDetailsRepository;
			_performanceLogRepository = performanceLogRepository;
			_voucherRepository = voucherRepository;
			_walletRepository = walletRepository;
		}

		public async Task<bool> create(CreateAppointment appointment)
		{
			try
			{
				// 1. Validate input
				if (!ValidateInput(appointment))
				{
					return false;
				}

				// 2. Resolve ServiceId
				int serviceId = await ResolveServiceId(appointment);
				if (serviceId == 0)
				{
					return false;
				}

				// 3. Get service and validate
				var service = await _serviceRepository.GetById(serviceId);
				if (service == null)
				{
					_logger.LogWarning("Create Appointment failed: Service not found {ServiceId}", serviceId);
					return false;
				}

				// 4. Get clinic information
				int clinicId = await GetClinicForStaff(appointment.StaffId!.Value);
				if (clinicId == 0)
				{
					return false;
				}

				var appointmentDate = appointment.StartTime!.Value.Date;

				// 5. Perform all validations
				var validationTasks = new List<Task<bool>>
			{
				ValidateDoctorLimits(appointment.StaffId.Value, serviceId, service, appointmentDate),
				ValidateClinicLimits(clinicId, appointmentDate),
				ValidateTimeLocks(clinicId, appointment.StartTime.Value)
			};

				var validationResults = await Task.WhenAll(validationTasks);
				if (validationResults.Any(result => !result))
				{
					return false;
				}

				// 6. Get next number order
				var nextNumberOrder = await GetNextNumberOrder(clinicId, appointmentDate);

				// 7. Create appointment and assignment
				var appointmentEntity = CreateAppointmentEntity(appointment, serviceId, service);
				var created = await _appointmentRepositoty.CreateEntity(appointmentEntity);

				if (!created)
				{
					_logger.LogError("Create Appointment failed at repository level");
					return false;
				}

				// 8. Create assignment
				var assignment = CreateAppointmentAssignment(
					appointmentEntity.Id,
					appointment.StaffId.Value,
					clinicId,
					service,
					appointment.StartTime.Value,
					nextNumberOrder);

				var assignmentCreated = await _appointmentAssignmentRepository.CreateEntity(assignment);
				if (!assignmentCreated)
				{
					_logger.LogWarning("Create AppointmentAssignment failed for AppointmentId {AppointmentId}", appointmentEntity.Id);
				}

				// 9. *** THÊM MỚI: Tạo hóa đơn và chi tiết hóa đơn ***
				if (appointment.TypeInvoice.HasValue && appointment.TypeInvoice != EnumTreatmentPlans.PayAfterService)
				{
					var invoiceId = await CreateInvoiceForAppointmentAsync(appointment, service, appointmentEntity.Id);
					if (invoiceId.HasValue)
					{
						// Cập nhật trạng thái thanh toán của appointment nếu đã thanh toán
						if (appointment.TypeInvoice == EnumTreatmentPlans.PayInAdvance)
						{
							appointmentEntity.PaymentStatus = true;
							await _appointmentRepositoty.UpdateEntity(appointmentEntity);
						}

						_logger.LogInformation("Created Invoice {InvoiceId} for AppointmentId {AppointmentId}", invoiceId, appointmentEntity.Id);
					}
				}

				// 10. Send confirmation email (fire and forget)
				_ = Task.Run(async () => await SendConfirmationEmail(appointmentEntity));

				_logger.LogInformation("Create Appointment success for CustomerId {CustomerId} with ServiceId {ServiceId} at ClinicId {ClinicId}",
					appointment.CustomerId, serviceId, clinicId);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create Appointment exception");
				return false;
			}
		}

		#region Invoice Creation Methods

		/// <summary>
		/// Tạo hóa đơn và chi tiết hóa đơn cho appointment (có hỗ trợ voucher)
		/// </summary>
		private async Task<int?> CreateInvoiceForAppointmentAsync(CreateAppointment appointment, ServiceEntity service, int appointmentId)
		{
			try
			{
				// Xác định giá và treatment plan
				decimal servicePrice = service.Price ?? 0;
				int? treatmentPlanId = null;

				// Nếu có CustomerTreatmentPlanId, lấy thông tin treatment plan
				if (appointment.CustomerTreatmentPlanId.HasValue)
				{
					var customerTreatmentPlan = await _customerTreatmentPlansRepository.GetById(appointment.CustomerTreatmentPlanId.Value);
					treatmentPlanId = customerTreatmentPlan?.TreatmentPlanId;

					if (treatmentPlanId.HasValue)
					{
						var treatmentPlan = await _treatmentPlanRepository.GetById(treatmentPlanId.Value);
						if (treatmentPlan?.Price.HasValue == true)
						{
							servicePrice = treatmentPlan.Price.Value;
						}
					}
				}

				// *** THÊM MỚI: Áp dụng voucher nếu có ***
				decimal discountValue = 0;
				int? appliedVoucherId = null;

				if (appointment.VoucherId.HasValue)
				{
					var voucherResult = await ApplyVoucherAsync(appointment.VoucherId.Value, appointment.CustomerId.Value, servicePrice);
					if (voucherResult.IsValid)
					{
						discountValue = voucherResult.DiscountAmount;
						appliedVoucherId = appointment.VoucherId.Value;
						_logger.LogInformation("Applied voucher {VoucherId} with discount {Discount:C} for CustomerId {CustomerId}",
							appointment.VoucherId, discountValue, appointment.CustomerId);
					}
					else
					{
						_logger.LogWarning("Failed to apply voucher {VoucherId} for CustomerId {CustomerId}: {Reason}",
							appointment.VoucherId, appointment.CustomerId, voucherResult.ErrorMessage);
					}
				}

				// Tính toán giá sau giảm giá
				decimal finalPrice = servicePrice - discountValue;

				// Tính toán số tiền thanh toán
				decimal paidAmount = appointment.TypeInvoice switch
				{
					EnumTreatmentPlans.PayInAdvance => finalPrice, // Trả trước toàn bộ (sau giảm giá)
					EnumTreatmentPlans.PartialPayment => Math.Min(appointment.PaidAmount ?? 0, finalPrice), // Trả một phần, không vượt quá tổng tiền
					_ => 0
				};

				// Xác định trạng thái hóa đơn
				string invoiceStatus = GetInvoiceStatus(paidAmount, finalPrice);

				// Tạo hóa đơn
				var invoice = new InvoiceEntity
				{
					CustomerId = appointment.CustomerId.Value,
					StaffId = appointment.StaffId.Value,
					ServiceId = service.Id,
					VoucherId = appliedVoucherId, // *** THÊM MỚI: Lưu VoucherId đã áp dụng ***
					TreatmentPlanId = treatmentPlanId,
					TotalMoney = finalPrice, // Tổng tiền sau giảm giá
					PaidAmount = paidAmount,
					OutstandingBalance = finalPrice - paidAmount,
					DateCreated = DateTime.UtcNow,
					Status = invoiceStatus,
					Type = "BanHang",
					OrderStatus = invoiceStatus,
					PaymentMethod = appointment.PaymentMethod,
					DiscountValue = discountValue, // *** THÊM MỚI: Lưu số tiền giảm giá ***
					DeleteStatus = false
				};

				var invoiceCreated = await _invoiceRepository.CreateEntity(invoice);
				if (!invoiceCreated)
				{
					_logger.LogError("CreateInvoiceForAppointment: Failed to create invoice for AppointmentId {AppointmentId}", appointmentId);
					return null;
				}

				// Tạo chi tiết hóa đơn
				var invoiceDetail = new InvoiceDetailEntity
				{
					InvoiceId = invoice.Id,
					ProductId = null, // Vì đây là dịch vụ, không phải sản phẩm
					ServiceId = service.Id,
					TreatmentPlanId = treatmentPlanId,
					VoucherId = appliedVoucherId, // Nếu có voucher áp dụng cho dòng này
					DiscountValue = discountValue, // Số tiền giảm giá (hoặc phần trăm, tùy logic)
					Price = servicePrice, // Giá gốc trước giảm giá
					Quantity = 1,
					TotalMoney = finalPrice, // Tổng tiền sau giảm giá
					Status = invoiceStatus, // Hoặc "DangCho" tùy trạng thái xử lý
					Type = "Ban", // "Ban" = bán hàng, "Nhap" = nhập hàng
					StatusComment = false, // Mặc định chưa đánh giá
					DeleteStatus = false
				};


				var detailCreated = await _invoiceDetailsRepository.CreateEntity(invoiceDetail);
				if (!detailCreated)
				{
					_logger.LogWarning("CreateInvoiceForAppointment: Failed to create invoice detail for InvoiceId {InvoiceId}", invoice.Id);
				}

				// *** THÊM MỚI: Đánh dấu voucher đã sử dụng ***
				if (appliedVoucherId.HasValue)
				{
					await MarkVoucherAsUsedAsync(appliedVoucherId.Value, appointment.CustomerId.Value);
				}

				// Tạo performance log cho nhân viên nếu có thanh toán
				if (paidAmount > 0 && appointment.StaffId.HasValue)
				{
					await CreatePerformanceLogAsync(invoice.Id, appointment.StaffId.Value, finalPrice);
				}

				return invoice.Id;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CreateInvoiceForAppointment: Exception for AppointmentId {AppointmentId}", appointmentId);
				return null;
			}
		}

		private string GetInvoiceStatus(decimal paidAmount, decimal totalAmount)
		{
			if (paidAmount >= totalAmount)
				return "DaThanhToan";    // Đã thanh toán đủ
			if (paidAmount > 0)
				return "ThanhToanMotPhan";  // Thanh toán một phần (hoặc có thể dùng "ChuaThanhToanDu")
			return "ChuaThanhToan";     // Chưa thanh toán
		}

		private async Task CreatePerformanceLogAsync(int invoiceId, int staffId, decimal totalAmount)
		{
			try
			{
				// Tính hoa hồng: 1.5% tổng hóa đơn
				decimal commission = totalAmount * 0.015m; // 1.5%

				// Tính thưởng dựa trên tổng tiền hóa đơn
				decimal bonus = totalAmount > 10_000_000m ? 500_000m : 200_000m;

				var log = new PerformanceLogEntity
				{
					InvoiceId = invoiceId,
					StaffId = staffId,
					Commission = commission,
					Bonus = bonus,
					LogDate = DateTime.UtcNow,
					Description = $"Hoa hồng {commission:N0} VNĐ (1.5%) và thưởng {bonus:N0} VNĐ từ hóa đơn #{invoiceId} - Tổng: {totalAmount:N0} VNĐ",
					DeleteStatus = false
				};

				var created = await _performanceLogRepository.CreateEntity(log);
				if (created)
				{
					_logger.LogInformation("CreatePerformanceLog: Created for StaffId {StaffId}, InvoiceId {InvoiceId} - Commission: {Commission:C}, Bonus: {Bonus:C}",
						staffId, invoiceId, commission, bonus);
				}
				else
				{
					_logger.LogError("CreatePerformanceLog: Failed to create performance log for StaffId {StaffId}, InvoiceId {InvoiceId}",
						staffId, invoiceId);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CreatePerformanceLogAsync: Exception for InvoiceId {InvoiceId}, StaffId {StaffId}",
					invoiceId, staffId);
			}
		}


		#endregion

		#region Voucher Methods

		/// <summary>
		/// Áp dụng voucher và tính toán giảm giá
		/// </summary>
		private async Task<(bool IsValid, decimal DiscountAmount, string ErrorMessage)> ApplyVoucherAsync(int voucherId, int customerId, decimal orderAmount)
		{
			try
			{
				// 1. Kiểm tra voucher có tồn tại và còn hoạt động
				var voucher = await _voucherRepository.GetById(voucherId);
				if (voucher == null || voucher.DeleteStatus)
				{
					return (false, 0, "Voucher không tồn tại");
				}

				if (!voucher.IsActive)
				{
					return (false, 0, "Voucher đã bị vô hiệu hóa");
				}

				// 2. Kiểm tra thời hạn voucher
				var now = DateTime.UtcNow;
				if (voucher.StartDate.HasValue && now < voucher.StartDate.Value)
				{
					return (false, 0, "Voucher chưa có hiệu lực");
				}

				if (voucher.EndDate.HasValue && now > voucher.EndDate.Value)
				{
					return (false, 0, "Voucher đã hết hạn");
				}

				// 3. Kiểm tra giá trị đơn hàng tối thiểu
				if (voucher.MinimumOrderValue.HasValue && orderAmount < voucher.MinimumOrderValue.Value)
				{
					return (false, 0, $"Đơn hàng tối thiểu {voucher.MinimumOrderValue.Value:C}");
				}

				// 4. Kiểm tra khách hàng có voucher trong ví và chưa sử dụng
				var walletVoucher = await _walletRepository.FindByPredicate(x =>
					x.CustomerId == customerId &&
					x.VoucherId == voucherId &&
					!x.IsUsed &&
					!x.DeleteStatus);

				var customerVoucher = walletVoucher.FirstOrDefault();
				if (customerVoucher == null)
				{
					return (false, 0, "Voucher không có trong ví hoặc đã được sử dụng");
				}

				// 5. Tính toán số tiền giảm giá
				decimal discountAmount = 0;
				if (voucher.DiscountValue.HasValue)
				{
					// Giảm giá theo phần trăm
					discountAmount = orderAmount * (voucher.DiscountValue.Value / 100);

					// Áp dụng giới hạn giảm giá tối đa nếu có
					if (voucher.MaxValue.HasValue && discountAmount > voucher.MaxValue.Value)
					{
						discountAmount = voucher.MaxValue.Value;
					}
				}

				// Đảm bảo số tiền giảm không vượt quá tổng đơn hàng
				if (discountAmount > orderAmount)
				{
					discountAmount = orderAmount;
				}

				return (true, discountAmount, string.Empty);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ApplyVoucherAsync: Exception for VoucherId {VoucherId}, CustomerId {CustomerId}", voucherId, customerId);
				return (false, 0, "Lỗi hệ thống khi áp dụng voucher");
			}
		}

		/// <summary>
		/// Đánh dấu voucher đã được sử dụng
		/// </summary>
		private async Task MarkVoucherAsUsedAsync(int voucherId, int customerId)
		{
			try
			{
				var walletVoucher = await _walletRepository.FindByPredicate(x =>
					x.CustomerId == customerId &&
					x.VoucherId == voucherId &&
					!x.IsUsed &&
					!x.DeleteStatus);

				var customerVoucher = walletVoucher.FirstOrDefault();
				if (customerVoucher != null)
				{
					customerVoucher.IsUsed = true;
					await _walletRepository.UpdateEntity(customerVoucher);

					_logger.LogInformation("MarkVoucherAsUsed: VoucherId {VoucherId} marked as used for CustomerId {CustomerId}",
						voucherId, customerId);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "MarkVoucherAsUsedAsync: Exception for VoucherId {VoucherId}, CustomerId {CustomerId}",
					voucherId, customerId);
			}
		}

		#endregion

		#region Private Helper Methods

		private bool ValidateInput(CreateAppointment appointment)
		{
			if (!appointment.CustomerId.HasValue)
			{
				_logger.LogWarning("Create Appointment failed: Missing CustomerId");
				return false;
			}

			if (!appointment.StartTime.HasValue)
			{
				_logger.LogWarning("Create Appointment failed: Missing StartTime");
				return false;
			}

			if (!appointment.StaffId.HasValue)
			{
				_logger.LogWarning("Create Appointment failed: Missing StaffId");
				return false;
			}

			return true;
		}

		private async Task<int> ResolveServiceId(CreateAppointment appointment)
		{
			if (appointment.ServiceId.HasValue)
			{
				return appointment.ServiceId.Value;
			}

			if (appointment.CustomerTreatmentPlanId.HasValue)
			{
				var serviceId = await GetServiceIdFromCustomerTreatmentPlan(appointment.CustomerTreatmentPlanId.Value);
				if (serviceId == 0)
				{
					_logger.LogWarning("Create Appointment failed: Cannot get ServiceId from CustomerTreatmentPlanId {CustomerTreatmentPlanId}",
						appointment.CustomerTreatmentPlanId);
					return 0;
				}
				return serviceId;
			}

			_logger.LogWarning("Create Appointment failed: Neither ServiceId nor CustomerTreatmentPlanId provided");
			return 0;
		}

		private async Task<int> GetClinicForStaff(int staffId)
		{
			var clinicStaff = (await _clinicStaffRepository
				.FindByPredicate(x => x.StaffId == staffId))
				.FirstOrDefault();

			if (!clinicStaff?.ClinicId.HasValue ?? true)
			{
				_logger.LogWarning("Create Appointment failed: Clinic not found for StaffId {StaffId}", staffId);
				return 0;
			}

			return clinicStaff.ClinicId.Value;
		}

		private async Task<bool> ValidateDoctorLimits(int staffId, int serviceId, ServiceEntity service, DateTime date)
		{
			if (service.IsCourse == (int)EnumTypeCourse.Package)
			{
				var isWithinLimit = await CheckDoctorDailyLimit(staffId, serviceId, date);
				if (!isWithinLimit)
				{
					_logger.LogWarning("Create Appointment failed: Doctor daily limit exceeded ({Limit}) for StaffId {StaffId}, ServiceId {ServiceId} on date {Date}",
						MAX_DOCTOR_DAILY_LIMIT, staffId, serviceId, date.ToString("yyyy-MM-dd"));
					return false;
				}
			}
			return true;
		}

		private async Task<bool> ValidateClinicLimits(int clinicId, DateTime date)
		{
			var existingCount = await GetClinicAppointmentCount(clinicId, date);
			if (existingCount >= MAX_CLINIC_DAILY_LIMIT)
			{
				_logger.LogWarning("Create Appointment failed: Daily limit exceeded ({Limit}) for ClinicId {ClinicId} on date {Date}",
					MAX_CLINIC_DAILY_LIMIT, clinicId, date.ToString("yyyy-MM-dd"));
				return false;
			}
			return true;
		}

		private async Task<bool> ValidateTimeLocks(int clinicId, DateTime startTime)
		{
			var locks = await _appointmentTimeLockRepository.FindByPredicate(x =>
				x.DeleteStatus == false &&
				x.StartTime <= startTime &&
				x.EndTime >= startTime &&
				x.ClinicId == clinicId);

			if (locks.Any())
			{
				_logger.LogWarning("Create Appointment failed: Time or clinic locked");
				return false;
			}
			return true;
		}

		private async Task<int> GetNextNumberOrder(int clinicId, DateTime date)
		{
			var existingAssignments = await _appointmentAssignmentRepository.FindByPredicate(x =>
				x.ClinicId == clinicId &&
				x.AssignedDate.HasValue &&
				x.AssignedDate.Value.Date == date &&
				!x.DeleteStatus);

			return existingAssignments.Any()
				? existingAssignments.Max(x => x.NumberOrder ?? 0) + 1
				: 1;
		}

		private async Task<int> GetClinicAppointmentCount(int clinicId, DateTime date)
		{
			var existingAssignments = await _appointmentAssignmentRepository.FindByPredicate(x =>
				x.ClinicId == clinicId &&
				x.AssignedDate.HasValue &&
				x.AssignedDate.Value.Date == date &&
				!x.DeleteStatus);

			return existingAssignments.Count();
		}

		private AppointmentEntity CreateAppointmentEntity(CreateAppointment appointment, int serviceId, ServiceEntity service)
		{
			int? customerTreatmentPlanId = service.IsCourse == (int)EnumTypeCourse.Package
				? appointment.CustomerTreatmentPlanId
				: null;

			return new AppointmentEntity
			{
				CustomerId = appointment.CustomerId,
				StaffId = appointment.StaffId,
				ServiceId = serviceId,
				StartTime = appointment.StartTime,
				CustomerTreatmentPlanId = customerTreatmentPlanId,
				Status = (int)AppointmentStatus.Booked,
				PaymentStatus = false,
				DeleteStatus = false,
				CreationDate = DateTime.Now,
				IsConfirmationEmailSent = false,
				IsReminderEmailSent = false,
				ReminderHoursBefore = DEFAULT_REMINDER_HOURS
			};
		}

		#endregion

		#region Existing Methods (kept as-is for compatibility)

		private async Task<int> GetServiceIdFromCustomerTreatmentPlan(int customerTreatmentPlanId)
		{
			try
			{
				var customerPlan = await _customerTreatmentPlansRepository.GetById(customerTreatmentPlanId);
				if (customerPlan?.TreatmentPlanId == null)
				{
					_logger.LogWarning("GetServiceIdFromCustomerTreatmentPlan: CustomerTreatmentPlan not found or TreatmentPlanId is null. Id: {Id}", customerTreatmentPlanId);
					return 0;
				}

				var treatmentPlan = await _treatmentPlanRepository.GetById(customerPlan.TreatmentPlanId.Value);
				if (treatmentPlan?.ServiceId == null)
				{
					_logger.LogWarning("GetServiceIdFromCustomerTreatmentPlan: TreatmentPlan not found or ServiceId is null. TreatmentPlanId: {Id}", customerPlan.TreatmentPlanId);
					return 0;
				}

				return treatmentPlan.ServiceId.Value;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetServiceIdFromCustomerTreatmentPlan: Exception for CustomerTreatmentPlanId {Id}", customerTreatmentPlanId);
				return 0;
			}
		}

		private async Task<bool> CheckDoctorDailyLimit(int staffId, int serviceId, DateTime date)
		{
			try
			{
				var existingAssignments = await _appointmentAssignmentRepository.FindByPredicate(x =>
					x.StaffId == staffId &&
					x.ServiceId == serviceId &&
					x.AssignedDate.HasValue &&
					x.AssignedDate.Value.Date == date &&
					!x.DeleteStatus);

				var count = existingAssignments.Count();
				_logger.LogInformation("CheckDoctorDailyLimit: StaffId {StaffId}, ServiceId {ServiceId}, Date {Date}, Current count: {Count}",
					staffId, serviceId, date.ToString("yyyy-MM-dd"), count);

				return count < MAX_DOCTOR_DAILY_LIMIT;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CheckDoctorDailyLimit: Exception for StaffId {StaffId}, ServiceId {ServiceId}, Date {Date}",
					staffId, serviceId, date.ToString("yyyy-MM-dd"));
				return false;
			}
		}

		private AppointmentAssignmentEntity CreateAppointmentAssignment(
			int appointmentId,
			int staffId,
			int clinicId,
			ServiceEntity service,
			DateTime startTime,
			int numberOrder)
		{
			var assignment = new AppointmentAssignmentEntity
			{
				AppointmentId = appointmentId,
				StaffId = staffId,
				ClinicId = clinicId,
				ServiceId = service.Id,
				ServiceTypeId = service.ServiceTypeId,
				AssignedDate = startTime,
				Status = false,
				QuantityServices = 1,
				Price = service.Price,
				PaymentStatus = false,
				NumberOrder = numberOrder,
				DeleteStatus = false
			};

			var serviceType = service.IsCourse == (int)EnumTypeCourse.Single ? "Single" : "Package";
			_logger.LogInformation("Created {ServiceType} service assignment for ServiceId {ServiceId}", serviceType, service.Id);

			return assignment;
		}

		private async Task SendConfirmationEmail(AppointmentEntity appointment)
		{
			try
			{
				var customer = await _customerRepository.GetById(appointment.CustomerId.Value);
				if (customer == null || string.IsNullOrEmpty(customer.Email))
				{
					_logger.LogWarning("SendConfirmationEmail: Customer not found or no email. CustomerId {CustomerId}", appointment.CustomerId);
					return;
				}

				var staff = await _staffRepository.GetById(appointment.StaffId.Value);
				var staffName = staff?.FullName ?? "Nhân viên";

				var service = await _serviceRepository.GetById(appointment.ServiceId.Value);
				var serviceName = service?.ServiceName ?? "Dịch vụ";

				var emailSent = await _emailService.SendAppointmentConfirmation(
					customer.Email,
					customer.FullName ?? "Khách hàng",
					serviceName,
					appointment.StartTime.Value,
					staffName
				);

				if (emailSent)
				{
					appointment.IsConfirmationEmailSent = true;
					appointment.ConfirmationEmailSentDate = DateTime.UtcNow;
					await _appointmentRepositoty.UpdateEntity(appointment);

					_logger.LogInformation("SendConfirmationEmail: Successfully sent to {Email} for AppointmentId {AppointmentId}",
						customer.Email, appointment.Id);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SendConfirmationEmail: Exception for AppointmentId {AppointmentId}", appointment.Id);
			}
		}

		#endregion

		public async Task<bool> delete(DeleteAppointment appointment)
		{
			try
			{
				_logger.LogInformation("Start deleting Appointment");

				if (!appointment.Id.HasValue)
				{
					_logger.LogWarning("Delete Appointment failed: Missing Id");
					return false;
				}

				// 1. Get existing appointment
				var existingAppointment = await _appointmentRepositoty.GetById(appointment.Id.Value);
				if (existingAppointment == null)
				{
					_logger.LogWarning("Delete Appointment failed: Not found with Id {Id}", appointment.Id);
					return false;
				}

				// 2. Check if appointment can be cancelled (business rule)
				if (!CanCancelAppointment(existingAppointment))
				{
					_logger.LogWarning("Delete Appointment failed: Cannot cancel appointment with Id {Id} due to business rules", appointment.Id);
					return false;
				}

				// 3. Soft delete related assignment first
				var assignments = await _appointmentAssignmentRepository.FindByPredicate(x =>
					x.AppointmentId == appointment.Id.Value && !x.DeleteStatus);

				if (assignments.Any())
				{
					foreach (var assignment in assignments)
					{
						await _appointmentAssignmentRepository.DeleteEntitiesStatus(assignment);
					}
				}

				// 4. Soft delete the appointment
				var deleted = await _appointmentRepositoty.DeleteEntitiesStatus(existingAppointment);
				if (!deleted)
				{
					_logger.LogError("Delete Appointment failed at repository level: Id {Id}", appointment.Id);
					return false;
				}

				// 5. Send cancellation email (fire and forget)
				_ = Task.Run(async () => await SendCancellationEmail(existingAppointment));

				_logger.LogInformation("Delete Appointment success: Id {Id} for CustomerId {CustomerId}",
					appointment.Id, existingAppointment.CustomerId);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete Appointment exception: Id {Id}", appointment.Id);
				return false;
			}
		}

		public async Task<BaseDataCollection<AppointmentEntity>> getlist(AppointmentGet appointment)
		{
			try
			{
				_logger.LogInformation("Getting Appointment list with filters");

				// Build predicate based on search criteria
				Expression<Func<AppointmentEntity, bool>> predicate = x => !x.DeleteStatus;

				if (appointment.CustomerId.HasValue)
				{
					var customerPredicate = predicate;
					predicate = x => customerPredicate.Compile()(x) && x.CustomerId == appointment.CustomerId.Value;
				}

				if (appointment.StaffId.HasValue)
				{
					var staffPredicate = predicate;
					predicate = x => staffPredicate.Compile()(x) && x.StaffId == appointment.StaffId.Value;
				}

				// Get all matching records
				var allMatching = await _appointmentRepositoty.FindByPredicate(predicate);
				var totalCount = allMatching.Count();

				// Apply pagination
				var pageSize = 10; // Default page size, could be configurable
				var pageNo = appointment.PageNo > 0 ? appointment.PageNo : 1;
				var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

				var pagedData = allMatching
					.OrderByDescending(x => x.CreationDate ?? DateTime.MinValue)
					.ThenByDescending(x => x.StartTime ?? DateTime.MinValue)
					.Skip((pageNo - 1) * pageSize)
					.Take(pageSize)
					.ToList();

				var result = new BaseDataCollection<AppointmentEntity>
				{
					BaseDatas = pagedData,
					PageIndex = pageNo,
					PageCount = totalPages
				};

				_logger.LogInformation("GetList Appointment success: Found {Count} records (Page {PageNo}/{TotalPages})",
					totalCount, pageNo, totalPages);

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList Appointment exception");
				return new BaseDataCollection<AppointmentEntity>
				{
					BaseDatas = new List<AppointmentEntity>(),
					PageIndex = 0,
					PageCount = 0
				};
			}
		}

		#region Private Helper Methods for Delete

		private bool CanCancelAppointment(AppointmentEntity appointment)
		{
			// Business rules for cancellation
			if (appointment.StartTime.HasValue)
			{
				// Cannot cancel if appointment is within 2 hours
				var timeUntilAppointment = appointment.StartTime.Value - DateTime.Now;
				if (timeUntilAppointment.TotalHours < 2)
				{
					_logger.LogWarning("CanCancelAppointment: Too close to appointment time. StartTime: {StartTime}",
						appointment.StartTime.Value);
					return false;
				}
			}

			// Cannot cancel if already completed or in progress
			if (appointment.Status == (int)AppointmentStatus.Completed ||
				appointment.Status == (int)AppointmentStatus.InProgress)
			{
				_logger.LogWarning("CanCancelAppointment: Cannot cancel completed or in-progress appointment. Status: {Status}",
					appointment.Status);
				return false;
			}

			return true;
		}

		private async Task SendCancellationEmail(AppointmentEntity appointment)
		{
			try
			{
				var customer = await _customerRepository.GetById(appointment.CustomerId.Value);
				if (customer == null || string.IsNullOrEmpty(customer.Email))
				{
					_logger.LogWarning("SendCancellationEmail: Customer not found or no email. CustomerId {CustomerId}",
						appointment.CustomerId);
					return;
				}

				var staff = await _staffRepository.GetById(appointment.StaffId.Value);
				var staffName = staff?.FullName ?? "Nhân viên";

				var service = await _serviceRepository.GetById(appointment.ServiceId.Value);
				var serviceName = service?.ServiceName ?? "Dịch vụ";

				var emailSent = await _emailService.SendAppointmentCancellation(
					customer.Email,
					customer.FullName ?? "Khách hàng",
					serviceName,
					appointment.StartTime.Value,
					staffName
				);

				if (emailSent)
				{
					_logger.LogInformation("SendCancellationEmail: Successfully sent cancellation email to {Email} for AppointmentId {AppointmentId}",
						customer.Email, appointment.Id);
				}
				else
				{
					_logger.LogWarning("SendCancellationEmail: Failed to send cancellation email to {Email} for AppointmentId {AppointmentId}",
						customer.Email, appointment.Id);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SendCancellationEmail: Exception for AppointmentId {AppointmentId}", appointment.Id);
			}
		}
		#endregion

	}
}
