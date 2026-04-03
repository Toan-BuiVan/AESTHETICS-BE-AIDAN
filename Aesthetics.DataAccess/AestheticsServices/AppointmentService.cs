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
		private readonly ICustomerTreatmentSessionsRepository _customerTreatmentSessionRepository;
		private readonly ITreatmentSessionRepository _treatmentSessionRepository;
		private readonly IClinicRepository _clinicRepository;

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
			ICustomerTreatmentSessionsRepository customerTreatmentSessionRepository, 
			ITreatmentSessionRepository treatmentSessionRepository,
			IClinicRepository clinicRepository,
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
			_customerTreatmentSessionRepository = customerTreatmentSessionRepository;  
			_treatmentSessionRepository = treatmentSessionRepository;
			_clinicRepository = clinicRepository;
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
				_logger.LogInformation("CREATE_APPOINTMENT_START: Begin creating appointment");
				if (!ValidateBasicInput(appointment))
				{
					_logger.LogWarning("VALIDATE_INPUT_FAILED: Basic input validation failed");
					return false;
				}

				_logger.LogInformation("RESOLVE_CTS: Resolving CustomerTreatmentSession");

				int customerTreatmentSessionId = await ResolveCustomerTreatmentSessionId(appointment);
				if (customerTreatmentSessionId == 0)
				{
					_logger.LogWarning("RESOLVE_CTS_FAILED: Failed to resolve CustomerTreatmentSessionId");
					return false;
				}

				var customerTreatmentSession = await _customerTreatmentSessionRepository.GetById(customerTreatmentSessionId);
				if (customerTreatmentSession == null)
				{
					_logger.LogWarning("CTS_NOT_FOUND: CustomerTreatmentSession not found: {CTSId}", customerTreatmentSessionId);
					return false;
				}

				_logger.LogInformation("CTS_FOUND: CTS found with ID {CTSId}, Status: {Status}",
					customerTreatmentSessionId, customerTreatmentSession.Status);

				_logger.LogInformation("GET_TREATMENT_SESSION: Get TreatmentSession and Duration");

				var treatmentSession = await _treatmentSessionRepository.GetById(customerTreatmentSession.TreatmentSessionId.Value);
				if (treatmentSession == null)
				{
					_logger.LogWarning("TREATMENT_SESSION_NOT_FOUND: TreatmentSession not found");
					return false;
				}

				int serviceDuration = treatmentSession.Duration ?? 60;
				_logger.LogInformation("TREATMENT_SESSION_OK: SessionName: {SessionName}, Duration: {Duration}p",
					treatmentSession.SessionName, serviceDuration);

				_logger.LogInformation("GET_SERVICE_DETAILS: Get Service Details");

				var treatmentPlan = await _treatmentPlanRepository.GetById(treatmentSession.TreatmentPlanId.Value);
				if (treatmentPlan == null || treatmentPlan.ServiceId == null)
				{
					_logger.LogWarning("TREATMENT_PLAN_NOT_FOUND: TreatmentPlan or ServiceId not found");
					return false;
				}

				var service = await _serviceRepository.GetById(treatmentPlan.ServiceId.Value);
				if (service == null)
				{
					_logger.LogWarning("SERVICE_NOT_FOUND: Service not found");
					return false;
				}

				_logger.LogInformation("SERVICE_OK: ServiceName: {ServiceName}", service.ServiceName);

				_logger.LogInformation("COMPREHENSIVE_VALIDATION: Running Comprehensive Validation");

				var validationResult = await ValidateAppointmentCreation(
					appointment,
					service,
					treatmentSession,
					customerTreatmentSession,
					serviceDuration);

				if (!validationResult.IsValid)
				{
					_logger.LogWarning("VALIDATION_FAILED: {ErrorMessage}", validationResult.ErrorMessage);
					return false;
				}

				_logger.LogInformation("VALIDATION_PASSED: All Validations PASSED");

				_logger.LogInformation("GET_CLINIC: Get Clinic for Staff");

				int clinicId = await GetClinicForStaff(appointment.StaffId.Value);
				if (clinicId == 0)
				{
					_logger.LogWarning("CLINIC_NOT_FOUND: Clinic not found for doctor");
					return false;
				}

				_logger.LogInformation("CLINIC_OK: Clinic found with ID {ClinicId}", clinicId);

				var appointmentDate = appointment.StartTime.Value.Date;
				var nextNumberOrder = await GetNextNumberOrder(clinicId, appointmentDate);

				_logger.LogInformation("NUMBER_ORDER_OK: NextNumberOrder: {Order}", nextNumberOrder);

				_logger.LogInformation("CREATE_APPOINTMENT_ENTITY: Creating Appointment Entity");

				var appointmentEntity = new AppointmentEntity
				{
					CustomerId = appointment.CustomerId,
					StaffId = appointment.StaffId,
					ServiceId = treatmentPlan.ServiceId,
					CustomerTreatmentPlanId = appointment.CustomerTreatmentPlanId,
					CustomerTreatmentSessionId = customerTreatmentSessionId,  
					StartTime = appointment.StartTime,
					Status = (int)AppointmentStatus.Booked,
					PaymentStatus = appointment.TypeInvoice.HasValue ? (int?)appointment.TypeInvoice.Value : 0,
					DeleteStatus = false,
					CreationDate = DateTime.UtcNow,
					IsConfirmationEmailSent = false,
					IsReminderEmailSent = false,
					ReminderHoursBefore = DEFAULT_REMINDER_HOURS
				};

				var created = await _appointmentRepositoty.CreateEntity(appointmentEntity);
				if (!created)
				{
					_logger.LogError("CREATE_APPOINTMENT_FAILED: Failed to create appointment entity");
					return false;
				}

				_logger.LogInformation("APPOINTMENT_CREATED: Appointment created with ID {AppointmentId}", appointmentEntity.Id);

				_logger.LogInformation("CREATE_ASSIGNMENT: Creating Appointment Assignment");

				var assignment = CreateAppointmentAssignment(
					appointmentEntity.Id,
					appointment.StaffId ?? 0,
					clinicId,
					service,
					appointment.StartTime.Value,
					nextNumberOrder,
					appointmentEntity.PaymentStatus ?? 0,
					appointmentEntity.Status ?? 0);

				var assignmentCreated = await _appointmentAssignmentRepository.CreateEntity(assignment);
				if (!assignmentCreated)
				{
					_logger.LogWarning("ASSIGNMENT_FAILED: Failed to create appointment assignment");
				}
				else
				{
					_logger.LogInformation("ASSIGNMENT_CREATED: Assignment created successfully");
				}

				_logger.LogInformation("UPDATE_CTS_STATUS: Updating CustomerTreatmentSession Status");

				customerTreatmentSession.Status = "DaDatLich";
				var statusUpdated = await _customerTreatmentSessionRepository.UpdateEntity(customerTreatmentSession);
				if (statusUpdated)
				{
					_logger.LogInformation("CTS_STATUS_UPDATED: CTS Status updated to 'DaDatLich'");
				}
				else
				{
					_logger.LogWarning("CTS_STATUS_UPDATE_FAILED: Failed to update CTS status");
				}

				_logger.LogInformation("CREATE_INVOICE: Creating Invoice");

				var invoiceId = await CreateInvoiceForAppointmentAsync(
					appointment,
					service,
					appointmentEntity.Id,
					treatmentSession.TreatmentPlanId,
					treatmentSession.Id);  // ✅ BỔSUNG: Truyền TreatmentSessionId

				_logger.LogInformation("SEND_EMAIL: Sending confirmation email");
				try
				{
					await SendConfirmationEmail(appointmentEntity);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "SEND_EMAIL_EXCEPTION: Failed to send confirmation email, but appointment was created successfully");
				}

				_logger.LogInformation("CREATE_APPOINTMENT_SUCCESS: Appointment ID: {AppointmentId}, CTS ID: {CTSID}, Time: {StartTime:yyyy-MM-dd HH:mm}",
					appointmentEntity.Id, customerTreatmentSessionId, appointmentEntity.StartTime);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CREATE_APPOINTMENT_EXCEPTION: Exception in create appointment");
				return false;
			}
		}


		public async Task<bool> delete(DeleteAppointment appointment)
		{
			try
			{
				_logger.LogInformation("DELETE_APPOINTMENT: Deleting appointment ID {AppointmentId}", appointment.Id);

				if (!appointment.Id.HasValue)
				{
					_logger.LogWarning("DELETE_ID_REQUIRED: Id is required");
					return false;
				}

				var existing = await _appointmentRepositoty.GetById(appointment.Id.Value);
				if (existing == null)
				{
					_logger.LogWarning("DELETE_NOT_FOUND: Appointment not found");
					return false;
				}

				var deleted = await _appointmentRepositoty.DeleteEntitiesStatus(existing);
				if (deleted)
				{
					_logger.LogInformation("DELETE_SUCCESS: Appointment deleted: ID {AppointmentId}", appointment.Id);
				}

				return deleted;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "DELETE_EXCEPTION: Exception in delete appointment");
				return false;
			}
		}

		public async Task<BaseDataCollection<AppointmentResponseModel>> getlist(AppointmentGet appointment)
		{
			try
			{
				_logger.LogInformation("GET_LIST_START: Get appointment list with details");

				Expression<Func<AppointmentEntity, bool>> predicate;

				if (appointment.CustomerId.HasValue && appointment.StaffId.HasValue)
				{
					var customerId = appointment.CustomerId.Value;
					var staffId = appointment.StaffId.Value;
					predicate = x => !x.DeleteStatus && x.CustomerId == customerId && x.StaffId == staffId;
					_logger.LogInformation("FILTER_CUSTOMER_STAFF: Filtering by CustomerId: {CustomerId}, StaffId: {StaffId}", customerId, staffId);
				}
				else if (appointment.CustomerId.HasValue)
				{
					var customerId = appointment.CustomerId.Value;
					predicate = x => !x.DeleteStatus && x.CustomerId == customerId;
					_logger.LogInformation("FILTER_CUSTOMER: Filtering by CustomerId: {CustomerId}", customerId);
				}
				else if (appointment.StaffId.HasValue)
				{
					var staffId = appointment.StaffId.Value;
					predicate = x => !x.DeleteStatus && x.StaffId == staffId;
					_logger.LogInformation("FILTER_STAFF: Filtering by StaffId: {StaffId}", staffId);
				}
				else
				{
					predicate = x => !x.DeleteStatus;
					_logger.LogInformation("FILTER_NONE: No specific filters applied");
				}

				var allMatching = await _appointmentRepositoty.FindByPredicate(predicate);

				// Apply date filters
				if (appointment.StartDate.HasValue)
				{
					var startDate = appointment.StartDate.Value.Date;
					allMatching = allMatching.Where(x => x.StartTime.HasValue && x.StartTime.Value.Date >= startDate).ToList();
					_logger.LogInformation("FILTER_START_DATE: Filtering by StartDate: {StartDate}", startDate);
				}

				if (appointment.EndDate.HasValue)
				{
					var endDate = appointment.EndDate.Value.Date;
					allMatching = allMatching.Where(x => x.StartTime.HasValue && x.StartTime.Value.Date <= endDate).ToList();
					_logger.LogInformation("FILTER_END_DATE: Filtering by EndDate: {EndDate}", endDate);
				}

				// Apply status filter
				if (!string.IsNullOrEmpty(appointment.Status) && appointment.Status != "null")
				{
					if (int.TryParse(appointment.Status, out int statusCode))
					{
						allMatching = allMatching.Where(x => x.Status == statusCode).ToList();
						_logger.LogInformation("FILTER_STATUS: Filtering by Status: {Status}", statusCode);
					}
				}

				var totalCount = allMatching.Count();

				var pagedAppointments = allMatching
					.OrderByDescending(x => x.StartTime ?? DateTime.MinValue)
					.Skip((appointment.PageNo - 1) * appointment.PageSize)
					.Take(appointment.PageSize)
					.ToList();

				var responseModels = new List<AppointmentResponseModel>();
				foreach (var appt in pagedAppointments)
				{
					var responseModel = await MapAppointmentToResponseModel(appt);
					responseModels.Add(responseModel);
				}

				var result = new BaseDataCollection<AppointmentResponseModel>
				{
					BaseDatas = responseModels,
					TotalRecordCount = totalCount,
					PageIndex = appointment.PageNo,
					PageCount = (int)Math.Ceiling((double)totalCount / appointment.PageSize)
				};

				_logger.LogInformation("GET_LIST_SUCCESS: Found {Count} appointments", totalCount);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_LIST_EXCEPTION: Exception in getlist");
				return new BaseDataCollection<AppointmentResponseModel>
				{
					BaseDatas = new List<AppointmentResponseModel>(),
					TotalRecordCount = 0,
					PageIndex = 0,
					PageCount = 0
				};
			}
		}

		public async Task<DoctorAvailabilityResponseModel?> GetDoctorAvailability(GetDoctorAvailabilityRequest request)
		{
			try
			{
				_logger.LogInformation("GET_DOCTOR_AVAILABILITY: DoctorId {DoctorId}, Date {Date:yyyy-MM-dd}",
					request.DoctorId, request.Date.Date);

				if (request.DoctorId <= 0)
					return null;

				var doctor = await _staffRepository.GetById(request.DoctorId);
				if (doctor == null || doctor.IsDoctor != true)
					return null;

				int? ctsId = request.CustomerTreatmentSessionId;
				int serviceDuration = 60;
				int serviceId = 0;
				string serviceName = null;

				if (ctsId.HasValue)
				{
					var cts = await _customerTreatmentSessionRepository.GetById(ctsId.Value);
					if (cts?.TreatmentSessionId.HasValue == true)
					{
						var ts = await _treatmentSessionRepository.GetById(cts.TreatmentSessionId.Value);
						serviceDuration = ts?.Duration ?? 60;

						if (ts?.TreatmentPlanId.HasValue == true)
						{
							var treatmentPlan = await _treatmentPlanRepository.GetById(ts.TreatmentPlanId.Value);
							if (treatmentPlan?.ServiceId.HasValue == true)
							{
								serviceId = treatmentPlan.ServiceId.Value;

								var service = await _serviceRepository.GetById(serviceId);
								if (service != null)
								{
									serviceName = service.ServiceName;
								}
							}
						}
					}
				}

				// ✅ Lấy clinic của doctor
				int clinicId = await GetClinicForStaff(request.DoctorId);
				_logger.LogInformation("GET_DOCTOR_AVAILABILITY_CLINIC: DoctorId {DoctorId}, ClinicId {ClinicId}",
					request.DoctorId, clinicId);

				// ✅ Appointments của doctor vào ngày đó
				var appointments = await _appointmentRepositoty.FindByPredicate(x =>
					x.StaffId == request.DoctorId &&
					x.StartTime!.Value.Date == request.Date.Date &&
					x.Status != 4 &&
					!x.DeleteStatus);

				_logger.LogInformation("GET_DOCTOR_AVAILABILITY_APPOINTMENTS: Found {Count} appointments - " +
					"DoctorId: {DoctorId}, Date: {Date:yyyy-MM-dd}",
					appointments.Count(), request.DoctorId, request.Date.Date);

				foreach (var apt in appointments)
				{
					_logger.LogInformation("GET_DOCTOR_AVAILABILITY_APPOINTMENT_DETAIL: " +
						"AppointmentId: {Id}, StartTime: {Start:yyyy-MM-dd HH:mm:ss}",
						apt.Id, apt.StartTime);
				}

				// ✅ Time locks của clinic vào ngày đó (KHÔNG lọc theo doctor, chỉ lọc theo clinic và ngày)
				var requestDate = request.Date.Date;
				var timeLocks = await _appointmentTimeLockRepository.FindByPredicate(x =>
					x.ClinicId == clinicId &&
					x.StartTime!.Value.Date == requestDate &&
					!x.DeleteStatus);

				_logger.LogInformation("GET_DOCTOR_AVAILABILITY_TIME_LOCKS: Found {Count} time locks - " +
					"ClinicId: {ClinicId}, Date: {Date:yyyy-MM-dd}",
					timeLocks.Count(), clinicId, requestDate);

				// ✅ Log chi tiết các time locks
				foreach (var timeLock in timeLocks)
				{
					_logger.LogInformation("GET_DOCTOR_AVAILABILITY_TIME_LOCK_DETAIL: " +
						"TimeLockId: {Id}, ClinicId: {ClinicId}, Start: {Start:yyyy-MM-dd HH:mm:ss}, " +
						"End: {End:yyyy-MM-dd HH:mm:ss}, IsOverloaded: {IsOverloaded}",
						timeLock.Id, timeLock.ClinicId, timeLock.StartTime, timeLock.EndTime, timeLock.IsOverloaded);
				}

				// ✅ Tính toán khung giờ trống
				var availableSlots = CalculateAvailableTimeSlots(
					appointments.ToList(),
					timeLocks.ToList(),
					serviceDuration,
					100,
					requestDate);

				_logger.LogInformation("GET_DOCTOR_AVAILABILITY_RESULT: Total {Total} available slots - " +
					"ServiceDuration: {Duration}m",
					availableSlots.Count, serviceDuration);

				return new DoctorAvailabilityResponseModel
				{
					DoctorId = request.DoctorId,
					DoctorName = doctor.FullName,
					ServiceId = serviceId,
					ServiceName = serviceName,
					ServiceDuration = serviceDuration,
					Date = requestDate,
					AvailableTimeSlots = availableSlots,
					TotalAvailableSlots = availableSlots.Count,
					CurrentAppointmentCount = appointments.Count(),
					MaxDailyLimit = MAX_DOCTOR_DAILY_LIMIT,
					RemainingSlots = MAX_DOCTOR_DAILY_LIMIT - appointments.Count(),
					IsLimitReached = appointments.Count() >= MAX_DOCTOR_DAILY_LIMIT
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_DOCTOR_AVAILABILITY_EXCEPTION: Exception in GetDoctorAvailability");
				return null;
			}
		}

		private List<AvailableTimeSlot> CalculateAvailableTimeSlots(
			List<AppointmentEntity> appointments,
			List<AppointmentTimeLockEntity> timeLocks,
			int serviceDuration,
			int limit,
			DateTime workDate)
		{
			var availableSlots = new List<AvailableTimeSlot>();
			int workStartHour = 8;
			int workEndHour = 17;
			int slotIntervalMinutes = 30;

			// ✅ Định nghĩa giờ nghỉ trưa: 12h00 - 13h00
			int lunchBreakStartHour = 12;
			int lunchBreakEndHour = 13;

			var busyTimes = new List<(DateTime Start, DateTime End)>();

			// ✅ Thêm appointments vào busy times
			foreach (var appointment in appointments)
			{
				if (appointment.StartTime.HasValue)
				{
					busyTimes.Add((appointment.StartTime.Value, appointment.StartTime.Value.AddMinutes(serviceDuration)));
					_logger.LogInformation("CALCULATE_AVAILABLE_SLOTS_APPOINTMENT: Added appointment busy time - " +
						"Start: {Start:yyyy-MM-dd HH:mm:ss}, End: {End:yyyy-MM-dd HH:mm:ss}",
						appointment.StartTime.Value, appointment.StartTime.Value.AddMinutes(serviceDuration));
				}
			}

			// ✅ Thêm time locks vào busy times
			foreach (var timeLock in timeLocks)
			{
				if (timeLock.StartTime.HasValue && timeLock.EndTime.HasValue)
				{
					busyTimes.Add((timeLock.StartTime.Value, timeLock.EndTime.Value));
					_logger.LogInformation("CALCULATE_AVAILABLE_SLOTS_TIME_LOCK: Added time lock busy time - " +
						"Start: {Start:yyyy-MM-dd HH:mm:ss}, End: {End:yyyy-MM-dd HH:mm:ss}",
						timeLock.StartTime.Value, timeLock.EndTime.Value);
				}
			}

			// ✅ Thêm giờ nghỉ trưa vào busy times
			var lunchBreakStart = workDate.AddHours(lunchBreakStartHour);
			var lunchBreakEnd = workDate.AddHours(lunchBreakEndHour);
			busyTimes.Add((lunchBreakStart, lunchBreakEnd));
			_logger.LogInformation("CALCULATE_AVAILABLE_SLOTS_LUNCH_BREAK: Added lunch break - " +
				"Start: {Start:yyyy-MM-dd HH:mm:ss}, End: {End:yyyy-MM-dd HH:mm:ss}",
				lunchBreakStart, lunchBreakEnd);

			// ✅ Tính toán khung giờ trống
			var currentTime = workDate.AddHours(workStartHour);
			while (currentTime.Hour < workEndHour && availableSlots.Count < limit)
			{
				var slotEndTime = currentTime.AddMinutes(serviceDuration);

				// ✅ Kiểm tra xung đột với busy times
				bool isConflict = busyTimes.Any(busy =>
					currentTime < busy.End && slotEndTime > busy.Start);

				if (slotEndTime.Hour <= workEndHour && !isConflict)
				{
					availableSlots.Add(new AvailableTimeSlot
					{
						StartTime = currentTime.ToString("HH:mm"),
						EndTime = slotEndTime.ToString("HH:mm"),
						IsAvailable = true
					});

					_logger.LogInformation("CALCULATE_AVAILABLE_SLOTS_SLOT_ADDED: Added available slot - " +
						"Start: {Start:HH:mm}, End: {End:HH:mm}",
						currentTime.ToString("HH:mm"), slotEndTime.ToString("HH:mm"));
				}
				else if (isConflict)
				{
					var conflictWith = busyTimes.FirstOrDefault(busy =>
						currentTime < busy.End && slotEndTime > busy.Start);
					_logger.LogInformation("CALCULATE_AVAILABLE_SLOTS_SLOT_SKIPPED_CONFLICT: Skipped slot due to conflict - " +
						"SlotStart: {SlotStart:HH:mm}, SlotEnd: {SlotEnd:HH:mm}, " +
						"ConflictStart: {ConflictStart:yyyy-MM-dd HH:mm:ss}, ConflictEnd: {ConflictEnd:yyyy-MM-dd HH:mm:ss}",
						currentTime.ToString("HH:mm"), slotEndTime.ToString("HH:mm"),
						conflictWith.Start, conflictWith.End);
				}

				currentTime = currentTime.AddMinutes(slotIntervalMinutes);
			}

			_logger.LogInformation("CALCULATE_AVAILABLE_SLOTS_COMPLETE: Calculated {TotalSlots} available slots - " +
				"ServiceDuration: {ServiceDuration}m, Date: {Date:yyyy-MM-dd}",
				availableSlots.Count, serviceDuration, workDate);

			return availableSlots;
		}

		#region VALIDATION METHODS

		private bool ValidateBasicInput(CreateAppointment appointment)
		{
			if (!appointment.CustomerId.HasValue)
			{
				_logger.LogWarning("VALIDATE_CUSTOMER_ID_REQUIRED: CustomerId is required");
				return false;
			}

			if (!appointment.StaffId.HasValue)
			{
				_logger.LogWarning("VALIDATE_STAFF_ID_REQUIRED: StaffId is required");
				return false;
			}

			if (!appointment.StartTime.HasValue)
			{
				_logger.LogWarning("VALIDATE_START_TIME_REQUIRED: StartTime is required");
				return false;
			}

			if (!appointment.CustomerTreatmentSessionId.HasValue &&
				(!appointment.CustomerTreatmentPlanId.HasValue || !appointment.SessionNumber.HasValue))
			{
				_logger.LogWarning("VALIDATE_CTS_INFO_REQUIRED: Must provide either CustomerTreatmentSessionId or (CustomerTreatmentPlanId + SessionNumber)");
				return false;
			}

			return true;
		}

		private async Task<int> ResolveCustomerTreatmentSessionId(CreateAppointment appointment)
		{
			if (appointment.CustomerTreatmentSessionId.HasValue)
			{
				return appointment.CustomerTreatmentSessionId.Value;
			}

			if (appointment.CustomerTreatmentPlanId.HasValue && appointment.SessionNumber.HasValue)
			{
				var customerPlan = await _customerTreatmentPlansRepository.GetById(appointment.CustomerTreatmentPlanId.Value);
				if (customerPlan == null)
				{
					_logger.LogWarning("CUSTOMER_PLAN_NOT_FOUND: CustomerTreatmentPlan not found: {PlanId}", appointment.CustomerTreatmentPlanId);
					return 0;
				}

				var treatmentPlan = await _treatmentPlanRepository.GetById(customerPlan.TreatmentPlanId.Value);
				if (treatmentPlan == null)
				{
					_logger.LogWarning("TREATMENT_PLAN_NOT_FOUND: TreatmentPlan not found");
					return 0;
				}

				var treatmentSession = treatmentPlan.TreatmentSessions
					?.FirstOrDefault(x => x.SessionNumber == appointment.SessionNumber.Value);
				if (treatmentSession == null)
				{
					_logger.LogWarning("SESSION_NOT_FOUND: TreatmentSession not found for session number {SessionNumber}",
						appointment.SessionNumber);
					return 0;
				}

				var customerTreatmentSession = customerPlan.CustomerTreatmentSessions
					?.FirstOrDefault(x => x.TreatmentSessionId == treatmentSession.Id);
				if (customerTreatmentSession == null)
				{
					_logger.LogWarning("CUSTOMER_SESSION_NOT_FOUND: CustomerTreatmentSession not found");
					return 0;
				}

				return customerTreatmentSession.Id;
			}

			return 0;
		}

		private async Task<(bool IsValid, string ErrorMessage)> ValidateAppointmentCreation(
			CreateAppointment appointment,
			ServiceEntity service,
			TreatmentSessionEntity treatmentSession,
			CustomerTreatmentSessionEntity customerTreatmentSession,
			int serviceDuration)
		{
			try
			{
				_logger.LogInformation("VALIDATION_START: Begin comprehensive validation");

				var appointmentTime = appointment.StartTime.Value;
				var appointmentEndTime = appointmentTime.AddMinutes(serviceDuration);

				_logger.LogInformation("VALIDATE_DOCTOR: Checking doctor existence");
				var doctor = await _staffRepository.GetById(appointment.StaffId.Value);
				if (doctor == null || doctor.DeleteStatus)
					return (false, "Doctor not found");
				if (doctor.IsDoctor != true)
					return (false, "Staff is not a doctor");
				_logger.LogInformation("VALIDATE_DOCTOR_OK: Doctor validated");

				_logger.LogInformation("VALIDATE_CLINIC: Checking clinic");
				var clinic = await GetClinicForStaff(appointment.StaffId.Value);
				if (clinic == 0)
					return (false, "Clinic not found");
				_logger.LogInformation("VALIDATE_CLINIC_OK: Clinic validated");

				_logger.LogInformation("VALIDATE_HOURS: Checking business hours");
				if (appointmentTime < DateTime.UtcNow)
					return (false, "Cannot book appointment in the past");
				if (appointmentTime.Hour < 8 || appointmentTime.Hour >= 17)
					return (false, "Outside working hours (08:00-17:00)");
				_logger.LogInformation("VALIDATE_HOURS_OK: {Time:HH:mm}", appointmentTime);

				_logger.LogInformation("VALIDATE_SLOT_FIT: Checking slot fits in working hours");
				if (appointmentEndTime.Hour > 17 || (appointmentEndTime.Hour == 17 && appointmentEndTime.Minute > 0))
					return (false, $"Appointment ends at {appointmentEndTime:HH:mm} which exceeds working hours");
				_logger.LogInformation("VALIDATE_SLOT_FIT_OK: {Start:HH:mm} - {End:HH:mm}", appointmentTime, appointmentEndTime);

				_logger.LogInformation("VALIDATE_TIME_LOCK: Checking time locks");
				var timeLocks = await _appointmentTimeLockRepository.FindByPredicate(x =>
					x.StartTime <= appointmentTime &&
					x.EndTime >= appointmentEndTime &&
					!x.DeleteStatus);
				if (timeLocks.Any())
					return (false, $"Time slot {appointmentTime:HH:mm} - {appointmentEndTime:HH:mm} is locked");
				_logger.LogInformation("VALIDATE_TIME_LOCK_OK: No time locks");

				_logger.LogInformation("VALIDATE_DOCTOR_LIMIT: Checking doctor daily limit");
				var doctorAppointmentsToday = await _appointmentRepositoty.FindByPredicate(x =>
					x.StaffId == appointment.StaffId.Value &&
					x.StartTime!.Value.Date == appointmentTime.Date &&
					x.Status != 4 &&
					!x.DeleteStatus);
				int doctorCount = doctorAppointmentsToday.Count();
				if (doctorCount >= MAX_DOCTOR_DAILY_LIMIT)
					return (false, $"Doctor has reached daily limit of {MAX_DOCTOR_DAILY_LIMIT} appointments");
				_logger.LogInformation("VALIDATE_DOCTOR_LIMIT_OK: {Count}/{Max}", doctorCount, MAX_DOCTOR_DAILY_LIMIT);

				_logger.LogInformation("VALIDATE_CLINIC_LIMIT: Checking clinic daily limit");
				var clinicAppointmentsToday = await _appointmentAssignmentRepository.FindByPredicate(x =>
					x.ClinicId == clinic &&
					x.AssignedDate!.Value.Date == appointmentTime.Date &&
					!x.DeleteStatus);
				int clinicCount = clinicAppointmentsToday.Count();
				if (clinicCount >= MAX_CLINIC_DAILY_LIMIT)
					return (false, $"Clinic has reached daily limit of {MAX_CLINIC_DAILY_LIMIT} appointments");
				_logger.LogInformation("VALIDATE_CLINIC_LIMIT_OK: {Count}/{Max}", clinicCount, MAX_CLINIC_DAILY_LIMIT);

				_logger.LogInformation("VALIDATE_CONFLICT: Checking conflicts with existing appointments");
				bool hasConflict = await CheckConflictWithExistingAppointments(
					appointment.StaffId.Value,
					appointmentTime,
					serviceDuration,
					appointmentTime.Date);
				if (hasConflict)
					return (false, "Time slot conflicts with existing appointment");
				_logger.LogInformation("VALIDATE_CONFLICT_OK: No conflicts");

				_logger.LogInformation("VALIDATE_CTS_STATUS: Checking CustomerTreatmentSession status");
				if (customerTreatmentSession.Status != "ChoDatLich" && customerTreatmentSession.Status != "ChuaThucHien")
					return (false, $"Session cannot be booked (Status: {customerTreatmentSession.Status})");
				_logger.LogInformation("VALIDATE_CTS_STATUS_OK: {Status}", customerTreatmentSession.Status);

				_logger.LogInformation("VALIDATION_SUCCESS: All validations passed");
				return (true, "OK");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "VALIDATION_EXCEPTION: Exception in validation");
				return (false, $"Error: {ex.Message}");
			}
		}

		#endregion

		#region HELPER METHODS

		private async Task<int> GetClinicForStaff(int staffId)
		{
			try
			{
				var clinicStaff = (await _clinicStaffRepository
					.FindByPredicate(x => x.StaffId == staffId && !x.DeleteStatus))
					.FirstOrDefault();

				if (clinicStaff?.ClinicId == null)
				{
					_logger.LogWarning("CLINIC_NOT_FOUND: Clinic not found for StaffId {StaffId}", staffId);
					return 0;
				}

				return clinicStaff.ClinicId.Value;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_CLINIC_EXCEPTION: Exception in GetClinicForStaff");
				return 0;
			}
		}

		private async Task<int> GetNextNumberOrder(int clinicId, DateTime date)
		{
			try
			{
				var existingAssignments = await _appointmentAssignmentRepository.FindByPredicate(x =>
					x.ClinicId == clinicId &&
					x.AssignedDate!.Value.Date == date &&
					!x.DeleteStatus);

				return existingAssignments.Any()
					? existingAssignments.Max(x => x.NumberOrder ?? 0) + 1
					: 1;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_NUMBER_ORDER_EXCEPTION: Exception in GetNextNumberOrder");
				return 1;
			}
		}

		private AppointmentAssignmentEntity CreateAppointmentAssignment(
		int appointmentId,
		int staffId,
		int clinicId,
		ServiceEntity service,
		DateTime startTime,
		int numberOrder,
		int paymentStatus,
		int status)
		{
			try
			{
				var assignment = new AppointmentAssignmentEntity
				{
					AppointmentId = appointmentId,
					StaffId = staffId,
					ClinicId = clinicId,
					ServiceId = service.Id,
					ServiceTypeId = service.ServiceTypeId,
					AssignedDate = startTime,
					Status = status,
					QuantityServices = 1,
					Price = service.Price,
					PaymentStatus = paymentStatus,
					NumberOrder = numberOrder,
					DeleteStatus = false
				};

				_logger.LogInformation("CREATE_ASSIGNMENT_ENTITY: Created AppointmentAssignment entity");
				return assignment;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CREATE_ASSIGNMENT_EXCEPTION: Exception in CreateAppointmentAssignment");
				throw;
			}
		}

		private async Task<int?> CreateInvoiceForAppointmentAsync(
			CreateAppointment appointment,
			ServiceEntity service,
			int appointmentId,
			int? treatmentPlanId = null,
			int? treatmentSessionId = null)  // ✅ BỔSUNG parameter
		{
			try
			{
				// ✅ Lấy giá từ TreatmentPlans.Price
				decimal servicePrice = 0;
				if (treatmentPlanId.HasValue)
				{
					var treatmentPlan = await _treatmentPlanRepository.GetById(treatmentPlanId.Value);
					if (treatmentPlan != null && treatmentPlan.Price.HasValue)
					{
						servicePrice = treatmentPlan.Price.Value;
						_logger.LogInformation("GET_PRICE_FROM_PLAN: Lấy giá từ TreatmentPlan - " +
							"PlanId: {PlanId}, PlanName: {PlanName}, Price: {Price}",
							treatmentPlanId, treatmentPlan.PlanName, servicePrice);
					}
					else
					{
						_logger.LogWarning("GET_PRICE_FROM_PLAN_FAILED: TreatmentPlan không tồn tại hoặc chưa có giá - " +
							"PlanId: {PlanId}, sử dụng giá mặc định từ Service",
							treatmentPlanId);
						servicePrice = service.Price ?? 0;
					}
				}
				else
				{
					servicePrice = service.Price ?? 0;
					_logger.LogInformation("GET_PRICE_FROM_SERVICE: Không có TreatmentPlan, lấy giá từ Service - " +
						"ServiceId: {ServiceId}, Price: {Price}",
						service.Id, servicePrice);
				}

				decimal discountValue = 0;
				int? appliedVoucherId = null;

				if (appointment.VoucherId.HasValue)
				{
					var voucher = await _voucherRepository.GetById(appointment.VoucherId.Value);
					if (voucher != null && voucher.IsActive == true && !voucher.DeleteStatus)
					{
						discountValue = servicePrice * (voucher.DiscountValue.Value / 100);
						if (voucher.MaxValue.HasValue && discountValue > voucher.MaxValue.Value)
							discountValue = voucher.MaxValue.Value;
						appliedVoucherId = appointment.VoucherId.Value;
					}
				}

				// ✅ Tính giá sau giảm
				decimal finalPrice = servicePrice - discountValue;
				decimal paidAmount = appointment.TypeInvoice == EnumTreatmentPlans.PayInAdvance
					? finalPrice
					: Math.Min(appointment.PaidAmount, finalPrice);

				string invoiceStatus = GetInvoiceStatus(paidAmount, finalPrice);

				// ✅ BỔSUNG: Lấy TreatmentSessionId từ CustomerTreatmentSession
				int? treatmentSessionIdFromCts = null;
				if (appointment.CustomerTreatmentSessionId.HasValue)
				{
					var customerTreatmentSession = await _customerTreatmentSessionRepository.GetById(appointment.CustomerTreatmentSessionId.Value);
					if (customerTreatmentSession != null)
					{
						treatmentSessionIdFromCts = customerTreatmentSession.TreatmentSessionId;
					}
				}

				var invoice = new InvoiceEntity
				{
					CustomerId = appointment.CustomerId.Value,
					StaffId = appointment.StaffId.Value,
					ServiceId = service.Id,
					VoucherId = appliedVoucherId,
					TreatmentPlanId = treatmentPlanId,        // ✅ BỔSUNG
					TreatmentSessionId = treatmentSessionId ?? treatmentSessionIdFromCts,  // ✅ BỔSUNG
					TotalMoney = servicePrice,        // ✅ Giá gốc
					DiscountValue = discountValue,     // ✅ Số tiền giảm
					FinalPrice = finalPrice,           // ✅ Giá sau giảm
					PaidAmount = paidAmount,
					OutstandingBalance = finalPrice - paidAmount,
					DateCreated = DateTime.UtcNow,
					Status = invoiceStatus,
					Type = "DichVu",
					OrderStatus = invoiceStatus,
					PaymentMethod = appointment.PaymentMethod,
					DeleteStatus = false
				};

				var invoiceCreated = await _invoiceRepository.CreateEntity(invoice);
				if (!invoiceCreated)
					return null;

				var invoiceDetail = new InvoiceDetailEntity
				{
					InvoiceId = invoice.Id,
					ServiceId = service.Id,
					Price = servicePrice,
					Quantity = 1,
					TreatmentPlanId = treatmentPlanId,        // ✅ BỔSUNG
					TreatmentSessionId = treatmentSessionId ?? treatmentSessionIdFromCts,  // ✅ BỔSUNG
					TotalMoney = servicePrice,        // ✅ Giá gốc
					DiscountValue = discountValue,     // ✅ Số tiền giảm
					FinalPrice = finalPrice,           // ✅ Giá sau giảm
					Status = invoiceStatus,
					Type = "DichVu",
					StatusComment = false,
					DeleteStatus = false
				};

				await _invoiceDetailsRepository.CreateEntity(invoiceDetail);
				return invoice.Id;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CREATE_INVOICE_EXCEPTION: Exception in CreateInvoiceForAppointmentAsync");
				return null;
			}
		}

		private string GetInvoiceStatus(decimal paidAmount, decimal totalAmount)
		{
			if (paidAmount >= totalAmount)
				return "DaThanhToan";
			if (paidAmount > 0)
				return "ThanhToanMotPhan";
			return "ChuaThanhToan";
		}

		private async Task SendConfirmationEmail(AppointmentEntity appointment)
		{
			try
			{
				var customer = await _customerRepository.GetById(appointment.CustomerId.Value);
				if (customer == null || string.IsNullOrEmpty(customer.Email))
				{
					_logger.LogWarning("CUSTOMER_EMAIL_NOT_FOUND: Customer email not found");
					return;
				}

				var staff = await _staffRepository.GetById(appointment.StaffId.Value);
				var service = await _serviceRepository.GetById(appointment.ServiceId.Value);

				var emailSent = await _emailService.SendAppointmentConfirmation(
					customer.Email,
					customer.FullName ?? "Customer",
					service?.ServiceName ?? "Service",
					appointment.StartTime.Value,
					staff?.FullName ?? "Staff"
				);

				if (emailSent)
				{
					appointment.IsConfirmationEmailSent = true;
					appointment.ConfirmationEmailSentDate = DateTime.UtcNow;
					await _appointmentRepositoty.UpdateEntity(appointment);
					_logger.LogInformation("CONFIRMATION_EMAIL_SENT: Confirmation email sent successfully");
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "CONFIRMATION_EMAIL_FAILED: Failed to send confirmation email");
			}
		}

		#endregion

		private async Task<AppointmentResponseModel> MapAppointmentToResponseModel(AppointmentEntity appointment)
		{
			try
			{
				var customer = await _customerRepository.GetById(appointment.CustomerId.Value);
				var staff = await _staffRepository.GetById(appointment.StaffId.Value);
				var service = await _serviceRepository.GetById(appointment.ServiceId.Value);

				TreatmentSessionEntity? treatmentSession = null;
				TreatmentPlanEntity? treatmentPlan = null;
				CustomerTreatmentPlanEntity? customerTreatmentPlan = null;
				
				if (appointment.CustomerTreatmentSessionId.HasValue)
				{
					var cts = await _customerTreatmentSessionRepository.GetById(appointment.CustomerTreatmentSessionId.Value);
					if (cts?.TreatmentSessionId.HasValue == true)
					{
						treatmentSession = await _treatmentSessionRepository.GetById(cts.TreatmentSessionId.Value);
						
						if (treatmentSession?.TreatmentPlanId.HasValue == true)
						{
							treatmentPlan = await _treatmentPlanRepository.GetById(treatmentSession.TreatmentPlanId.Value);
						}

						if (cts.CustomerTreatmentPlanId.HasValue)
						{
							customerTreatmentPlan = await _customerTreatmentPlansRepository.GetById(cts.CustomerTreatmentPlanId.Value);
						}
					}
				}

				DateTime? endTime = null;
				if (appointment.StartTime.HasValue && service?.Duration.HasValue == true)
				{
					endTime = appointment.StartTime.Value.AddMinutes(service.Duration.Value);
				}

				string purchaseType = "Lẻ";
				decimal? price = service?.Price;

				if (customerTreatmentPlan != null && treatmentPlan != null)
				{
					purchaseType = "Gói";
					price = treatmentPlan.Price;
				}

				var assignments = await _appointmentAssignmentRepository.FindByPredicate(x =>
					x.AppointmentId == appointment.Id && !x.DeleteStatus);
				var assignmentInfo = assignments.FirstOrDefault();

				return new AppointmentResponseModel
				{
					Id = appointment.Id,
					Customer = customer != null ? new CustomerInfo
					{
						Id = customer.Id,
						FullName = customer.FullName,			
						Email = customer.Email,
						PhoneNumber = customer.Phone,
						DateOfBirth = customer.DateBirth,
						Gender = customer.Sex
					} : null,
					Staff = staff != null ? new StaffInfo
					{
						Id = staff.Id,
						FullName = staff.FullName,
						Email = staff.Email,
						PhoneNumber = staff.Phone,
						Specialization = staff.Specialization,
						YearsOfExperience = staff.ExperienceYears
					} : null,
					Service = service != null ? new ServiceInfo
					{
						Id = service.Id,
						ServiceName = service.ServiceName,
						Duration = service.Duration,
						Price = service.Price,
						Description = service.Description
					} : null,
					TreatmentSession = treatmentSession != null ? new TreatmentSessionInfo
					{
						Id = treatmentSession.Id,
						SessionName = treatmentSession.SessionName,
						SessionNumber = treatmentSession.SessionNumber,
						Duration = treatmentSession.Duration,
						Description = treatmentSession.Description
					} : null,
					StartTime = appointment.StartTime,
					EndTime = endTime,
					Status = GetAppointmentStatusName(appointment.Status ?? 0),
					Price = price,
					PurchaseType = purchaseType,
					PaymentStatus = appointment.PaymentStatus ?? 0,
					CreationDate = appointment.CreationDate,
					IsConfirmationEmailSent = appointment.IsConfirmationEmailSent,
					IsReminderEmailSent = appointment.IsReminderEmailSent,
					ReminderHoursBefore = appointment.ReminderHoursBefore,
					Assignment = assignmentInfo != null ? await MapAssignmentInfo(assignmentInfo) : null
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "MAP_RESPONSE_EXCEPTION: Exception in MapAppointmentToResponseModel");
				return null;
			}
		}

		private async Task<AppointmentAssignmentInfo> MapAssignmentInfo(AppointmentAssignmentEntity assignment)
		{
			try
			{
				var clinic = await _clinicRepository.GetById(assignment.ClinicId ?? 0);	

				return new AppointmentAssignmentInfo
				{
					Id = assignment.Id,
					ClinicName = clinic?.ClinicName ?? "Unknown",
					NumberOrder = assignment.NumberOrder ?? 0,
					Price = assignment.Price,
					PaymentStatus = assignment.PaymentStatus ?? 0
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "MAP_ASSIGNMENT_EXCEPTION: Exception in MapAssignmentInfo");
				return null;
			}
		}

		

		private List<AvailableTimeSlot> CalculateAvailableTimeSlots(
			List<AppointmentEntity> appointments,
			List<AppointmentTimeLockEntity> timeLocks,
			int serviceDuration,
			int limit)
		{
			var availableSlots = new List<AvailableTimeSlot>();
			int workStartHour = 8;
			int workEndHour = 17;
			int slotIntervalMinutes = 30;

			// ✅ Định nghĩa giờ nghỉ trưa: 12h00 - 13h00
			int lunchBreakStartHour = 12;
			int lunchBreakEndHour = 13;

			DateTime workDate = appointments.FirstOrDefault()?.StartTime?.Date ??
								  timeLocks.FirstOrDefault()?.StartTime?.Date ??
								  DateTime.UtcNow.Date;

			var busyTimes = new List<(DateTime Start, DateTime End)>();
			foreach (var appointment in appointments)
			{
				if (appointment.StartTime.HasValue)
				{
					busyTimes.Add((appointment.StartTime.Value, appointment.StartTime.Value.AddMinutes(30)));
				}
			}

			foreach (var timeLock in timeLocks)
			{
				if (timeLock.StartTime.HasValue && timeLock.EndTime.HasValue)
				{
					busyTimes.Add((timeLock.StartTime.Value, timeLock.EndTime.Value));
				}
			}

			// ✅ Thêm giờ nghỉ trưa vào danh sách busy times
			var lunchBreakStart = workDate.AddHours(lunchBreakStartHour);
			var lunchBreakEnd = workDate.AddHours(lunchBreakEndHour);
			busyTimes.Add((lunchBreakStart, lunchBreakEnd));

			var currentTime = workDate.AddHours(workStartHour);
			while (currentTime.Hour < workEndHour && availableSlots.Count < limit)
			{
				var slotEndTime = currentTime.AddMinutes(serviceDuration);

				// ✅ Kiểm tra xung đột với busy times (bao gồm cả giờ nghỉ trưa)
				bool isConflict = busyTimes.Any(busy =>
					currentTime < busy.End && slotEndTime > busy.Start);

				if (slotEndTime.Hour <= workEndHour && !isConflict)
				{
					availableSlots.Add(new AvailableTimeSlot
					{
						StartTime = currentTime.ToString("HH:mm"),
						EndTime = slotEndTime.ToString("HH:mm"),
						IsAvailable = true
					});
				}

				currentTime = currentTime.AddMinutes(slotIntervalMinutes);
			}

			_logger.LogInformation("CALCULATE_AVAILABLE_SLOTS: Tính toán khung giờ khả dụng - " +
				"TotalSlots: {TotalSlots}, ServiceDuration: {ServiceDuration}m, " +
				"LunchBreakTime: {LunchBreakStart}h - {LunchBreakEnd}h",
				availableSlots.Count, serviceDuration, lunchBreakStartHour, lunchBreakEndHour);

			return availableSlots;
		}

		private async Task<bool> CheckConflictWithExistingAppointments(
			int staffId,
			DateTime appointmentTime,
			int serviceDuration,
			DateTime appointmentDate)
		{
			try
			{
				_logger.LogInformation("CONFLICT_CHECK: Checking conflicts with existing appointments");

				var appointmentsInDay = await _appointmentRepositoty.FindByPredicate(x =>
					x.StaffId == staffId &&
					x.StartTime!.Value.Date == appointmentDate &&
					x.Status != 4 &&
					!x.DeleteStatus);

				var appointmentEndTime = appointmentTime.AddMinutes(serviceDuration);

				foreach (var existingAppt in appointmentsInDay)
				{
					var existingService = await _serviceRepository.GetById(existingAppt.ServiceId.Value);
					if (existingService == null)
					{
						_logger.LogWarning("CONFLICT_SERVICE_NOT_FOUND: Service not found for existing appointment {AppointmentId}", existingAppt.Id);
						continue;
					}

					int existingDuration = existingService.Duration ?? 30;
					var existingEndTime = existingAppt.StartTime!.Value.AddMinutes(existingDuration);

					bool isOverlap = appointmentTime < existingEndTime && 
									 appointmentEndTime > existingAppt.StartTime!.Value;

					if (isOverlap)
					{
						_logger.LogWarning(
							"CONFLICT_FOUND: Request {ReqStart:HH:mm}-{ReqEnd:HH:mm} (duration: {ReqDuration}m) overlaps with existing {ExStart:HH:mm}-{ExEnd:HH:mm} (duration: {ExDuration}m)",
							appointmentTime.ToString("HH:mm"),
							appointmentEndTime.ToString("HH:mm"),
							serviceDuration,
							existingAppt.StartTime!.Value.ToString("HH:mm"),
							existingEndTime.ToString("HH:mm"),
							existingDuration);

						return true;
					}

					_logger.LogInformation(
						"CONFLICT_NO_OVERLAP: Request {ReqStart:HH:mm}-{ReqEnd:HH:mm} vs existing {ExStart:HH:mm}-{ExEnd:HH:mm}",
						appointmentTime.ToString("HH:mm"),
						appointmentEndTime.ToString("HH:mm"),
						existingAppt.StartTime!.Value.ToString("HH:mm"),
						existingEndTime.ToString("HH:mm"));
				}

				_logger.LogInformation("CONFLICT_CHECK_PASSED: No conflicts found");
				return false;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CONFLICT_CHECK_EXCEPTION: Exception in CheckConflictWithExistingAppointments");
				return false;
			}
		}



		/// <summary>
		/// CẬP NHẬT TRẠNG THÁI APPOINTMENT, CUSTOMERTREATMENTSESSION VÀ CUSTOMERTREATMENTPLAN
		/// 
		/// LUỒNG XỬ LÝ:
		/// 1. Validate input (CustomerTreatmentSessionId, Status)
		/// 2. Lấy CustomerTreatmentSession từ database
		/// 3. Tìm appointment liên quan
		/// 4. Cập nhật status của tất cả appointment (status = 1, 2, 3, 4)
		/// 5. Cập nhật status của CustomerTreatmentSession (mapping từ appointment status)
		/// 6. Lấy CustomerTreatmentPlan và cập nhật status dựa trên tất cả sessions:
		///    - Nếu có 1 session = "DangThucHien" → Plan = "DangThucHien"
		///    - Nếu tất cả session = "HoanThanh" → Plan = "HoanThanh"
		///    - Nếu tất cả session = "KhachHuy" → Plan = "KhachHuy"
		///    - Nếu chỉ có 1 session = "KhachHuy" nhưng có sessions khác → Plan không thay đổi
		/// 7. Log chi tiết mỗi bước
		/// 
		/// Status Mapping:
		/// Appointment Status → CustomerTreatmentSession Status
		/// 1 (Booked/DaDat) → "DaDatLich"
		/// 2 (InProgress/DangThucHien) → "DangThucHien"
		/// 3 (Completed/HoanThanh) → "HoanThanh"
		/// 4 (Cancelled/Huy) → "KhachHuy"
		/// </summary>
		/// <param name="request">Request chứa CustomerTreatmentSessionId và Status cần cập nhật</param>
		/// <returns>True nếu cập nhật thành công, False nếu lỗi</returns>
		public async Task<bool> UpdateAppointmentStatusAsync(updateappoint request)
		{
			try
			{
				// BƯỚC 1: Validate input
				if (request == null || !request.CustomerTreatmentSessionId.HasValue)
				{
					_logger.LogWarning("UPDATE_APPOINTMENT_STATUS_INVALID_INPUT: Request hoặc CustomerTreatmentSessionId không hợp lệ");
					return false;
				}

				if (!request.Status.HasValue)
				{
					_logger.LogWarning("UPDATE_APPOINTMENT_STATUS_INVALID_STATUS: Status không được để trống");
					return false;
				}

				int customerTreatmentSessionId = request.CustomerTreatmentSessionId.Value;
				int newStatus = request.Status.Value;

				// Validate status hợp lệ (1, 2, 3, 4 từ EnumAppointment)
				var validStatuses = new[] { 1, 2, 3, 4 };
				if (!validStatuses.Contains(newStatus))
				{
					_logger.LogWarning("UPDATE_APPOINTMENT_STATUS_INVALID_VALUE: Status không hợp lệ: {Status}. " +
						"Chỉ chấp nhận: 1 (Booked), 2 (InProgress), 3 (Completed), 4 (Cancelled)",
						newStatus);
					return false;
				}

				_logger.LogInformation("UPDATE_APPOINTMENT_STATUS_START: Cập nhật trạng thái - " +
					"CustomerTreatmentSessionId {SessionId}, NewStatus: {Status}",
					customerTreatmentSessionId, GetAppointmentStatusName(newStatus));

				if (customerTreatmentSessionId <= 0)
				{
					_logger.LogWarning("UPDATE_APPOINTMENT_STATUS_INVALID_ID: CustomerTreatmentSessionId không hợp lệ: {SessionId}",
						customerTreatmentSessionId);
					return false;
				}

				// BƯỚC 2: Lấy CustomerTreatmentSession
				var customerTreatmentSession = await _customerTreatmentSessionRepository.GetById(customerTreatmentSessionId);
				if (customerTreatmentSession == null || customerTreatmentSession.DeleteStatus)
				{
					_logger.LogWarning("UPDATE_APPOINTMENT_STATUS_SESSION_NOT_FOUND: CustomerTreatmentSession không tồn tại - SessionId {SessionId}",
						customerTreatmentSessionId);
					return false;
				}

				_logger.LogInformation("UPDATE_APPOINTMENT_STATUS_SESSION_FOUND: Tìm thấy session - Status cũ: {Status}, PlanId: {PlanId}",
					customerTreatmentSession.Status, customerTreatmentSession.CustomerTreatmentPlanId);

				// BƯỚC 3: Tìm appointment liên quan
				var appointments = await _appointmentRepositoty.FindByPredicate(x =>
					x.CustomerTreatmentSessionId == customerTreatmentSessionId &&
					!x.DeleteStatus);

				if (!appointments.Any())
				{
					_logger.LogWarning("UPDATE_APPOINTMENT_STATUS_NOT_FOUND: Không tìm thấy appointment nào - SessionId {SessionId}",
						customerTreatmentSessionId);

					// Nếu không có appointment, vẫn update status session và plan
					string sessionStatus = MapAppointmentStatusToSessionStatus(newStatus);
					customerTreatmentSession.Status = sessionStatus;
					var sessionUpdated = await _customerTreatmentSessionRepository.UpdateEntity(customerTreatmentSession);

					if (sessionUpdated)
					{
						_logger.LogInformation("UPDATE_APPOINTMENT_STATUS_SESSION_UPDATED_ONLY: Cập nhật session status thành công " +
							"(không có appointment) - SessionId {SessionId}, NewStatus: {Status}",
							customerTreatmentSessionId, sessionStatus);
						
						// Cập nhật Plan status
						if (customerTreatmentSession.CustomerTreatmentPlanId.HasValue)
						{
							await UpdateCustomerTreatmentPlanStatusAsync(customerTreatmentSession.CustomerTreatmentPlanId.Value);
						}
						
						return true;
					}

					_logger.LogError("UPDATE_APPOINTMENT_STATUS_SESSION_UPDATE_FAILED: Cập nhật session thất bại khi không có appointment");
					return false;
				}

				_logger.LogInformation("UPDATE_APPOINTMENT_STATUS_FOUND: Tìm thấy {Count} appointment cần cập nhật",
					appointments.Count());

				// BƯỚC 4: Cập nhật status của tất cả appointment
				int updatedCount = 0;
				foreach (var appointment in appointments)
				{
					int oldStatus = appointment.Status ?? 0;
					appointment.Status = newStatus;
					var updated = await _appointmentRepositoty.UpdateEntity(appointment);

					if (updated)
					{
						updatedCount++;
						_logger.LogInformation("UPDATE_APPOINTMENT_STATUS_UPDATED: Appointment status cập nhật - " +
							"AppointmentId {AppointmentId}, Status: {OldStatus} ({OldStatusName}) → {NewStatus} ({NewStatusName})",
							appointment.Id, oldStatus, GetAppointmentStatusName(oldStatus), newStatus, GetAppointmentStatusName(newStatus));
					}
					else
					{
						_logger.LogWarning("UPDATE_APPOINTMENT_STATUS_UPDATE_FAILED: Cập nhật appointment thất bại - AppointmentId {AppointmentId}",
							appointment.Id);
					}
				}

				// BƯỚC 5: Cập nhật status của CustomerTreatmentSession (map từ appointment status)
				string newSessionStatus = MapAppointmentStatusToSessionStatus(newStatus);
				string oldSessionStatus = customerTreatmentSession.Status;
				customerTreatmentSession.Status = newSessionStatus;
				var sessionStatusUpdated = await _customerTreatmentSessionRepository.UpdateEntity(customerTreatmentSession);

				if (!sessionStatusUpdated)
				{
					_logger.LogError("UPDATE_APPOINTMENT_STATUS_SESSION_UPDATE_FAILED: Cập nhật session status thất bại - SessionId {SessionId}",
						customerTreatmentSessionId);
					return false;
				}

				_logger.LogInformation("UPDATE_APPOINTMENT_STATUS_SESSION_UPDATED: Session status cập nhật thành công - " +
					"SessionId {SessionId}, Status: {OldStatus} → {NewStatus}",
					customerTreatmentSessionId, oldSessionStatus, newSessionStatus);

				// BƯỚC 6: Cập nhật status của CustomerTreatmentPlan dựa trên tất cả sessions
				bool planUpdated = false;
				if (customerTreatmentSession.CustomerTreatmentPlanId.HasValue)
				{
					planUpdated = await UpdateCustomerTreatmentPlanStatusAsync(customerTreatmentSession.CustomerTreatmentPlanId.Value);
				}

				// BƯỚC 7: Log kết quả hoàn tất
				_logger.LogInformation("UPDATE_APPOINTMENT_STATUS_SUCCESS: Cập nhật trạng thái hoàn tất - " +
					"SessionId {SessionId}, AppointmentUpdated: {UpdatedCount}/{TotalCount}, " +
					"AppointmentStatus: {AppointmentStatus} ({StatusName}), SessionStatus: {SessionStatus}, PlanUpdated: {PlanUpdated}",
					customerTreatmentSessionId, updatedCount, appointments.Count(), newStatus, 
					GetAppointmentStatusName(newStatus), newSessionStatus, planUpdated);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_APPOINTMENT_STATUS_EXCEPTION: Lỗi ngoại lệ khi cập nhật trạng thái - SessionId {SessionId}",
					request?.CustomerTreatmentSessionId ?? 0);
				return false;
			}
		}

		/// <summary>
		/// CẬP NHẬT STATUS CỦA CUSTOMERTREATMENTPLAN DỰA TRÊN TẤT CẢ SESSIONS
		/// 
		/// Logic:
		/// 1. Lấy tất cả sessions của plan
		/// 2. Nếu có 1 session = "DangThucHien" → Plan = "DangThucHien"
		/// 3. Nếu tất cả session = "HoanThanh" → Plan = "HoanThanh"
		/// 4. Nếu tất cả session = "KhachHuy" → Plan = "KhachHuy"
		/// 5. Nếu chỉ có 1 session = "KhachHuy" nhưng có sessions khác → Plan không thay đổi
		/// </summary>
		private async Task<bool> UpdateCustomerTreatmentPlanStatusAsync(int customerTreatmentPlanId)
		{
			try
			{
				_logger.LogInformation("UPDATE_PLAN_STATUS_START: Cập nhật status plan - PlanId {PlanId}",
					customerTreatmentPlanId);

				// Lấy CustomerTreatmentPlan
				var plan = await _customerTreatmentPlansRepository.GetById(customerTreatmentPlanId);
				if (plan == null || plan.DeleteStatus)
				{
					_logger.LogWarning("UPDATE_PLAN_STATUS_NOT_FOUND: CustomerTreatmentPlan không tồn tại - PlanId {PlanId}",
						customerTreatmentPlanId);
					return false;
				}

				string oldPlanStatus = plan.Status;

				// Lấy tất cả sessions của plan này
				var allSessions = await _customerTreatmentSessionRepository.FindByPredicate(x =>
					x.CustomerTreatmentPlanId == customerTreatmentPlanId &&
					!x.DeleteStatus);

				if (!allSessions.Any())
				{
					_logger.LogWarning("UPDATE_PLAN_STATUS_NO_SESSIONS: Không tìm thấy session nào - PlanId {PlanId}",
						customerTreatmentPlanId);
					return false;
				}

				_logger.LogInformation("UPDATE_PLAN_STATUS_SESSIONS_COUNT: Tìm thấy {Count} sessions", allSessions.Count());

				// Log tất cả session statuses
				var sessionStatuses = allSessions.Select(s => s.Status).Distinct().ToList();
				_logger.LogInformation("UPDATE_PLAN_STATUS_SESSION_STATUSES: Session statuses: {Statuses}",
					string.Join(", ", sessionStatuses));

				// Xác định status mới của plan dựa trên sessions
				string newPlanStatus = DetermineCustomerTreatmentPlanStatus(allSessions.ToList(), oldPlanStatus);

				_logger.LogInformation("UPDATE_PLAN_STATUS_DETERMINE: Trạng thái plan - Cũ: {OldStatus}, Mới: {NewStatus}",
					oldPlanStatus, newPlanStatus);

				// Nếu status không thay đổi, không cần update
				if (oldPlanStatus == newPlanStatus)
				{
					_logger.LogInformation("UPDATE_PLAN_STATUS_NO_CHANGE: Status không thay đổi - PlanId {PlanId}, Status: {Status}",
						customerTreatmentPlanId, oldPlanStatus);
					return true;
				}

				// Cập nhật plan
				plan.Status = newPlanStatus;
				var updated = await _customerTreatmentPlansRepository.UpdateEntity(plan);

				if (updated)
				{
					_logger.LogInformation("UPDATE_PLAN_STATUS_SUCCESS: Cập nhật status plan thành công - " +
						"PlanId {PlanId}, Status: {OldStatus} → {NewStatus}",
						customerTreatmentPlanId, oldPlanStatus, newPlanStatus);
					return true;
				}

				_logger.LogError("UPDATE_PLAN_STATUS_FAILED: Cập nhật status plan thất bại - PlanId {PlanId}",
					customerTreatmentPlanId);
				return false;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_PLAN_STATUS_EXCEPTION: Lỗi ngoại lệ - PlanId {PlanId}",
					customerTreatmentPlanId);
				return false;
			}
		}

		/// <summary>
		/// Xác định status của CustomerTreatmentPlan dựa trên tất cả sessions
		/// 
		/// Logic:
		/// 1. Nếu có 1 session = "DangThucHien" → Plan = "DangThucHien"
		/// 2. Nếu tất cả session = "HoanThanh" → Plan = "HoanThanh"
		/// 3. Nếu tất cả session = "KhachHuy" → Plan = "KhachHuy" (chỉ khi TẤT CẢ bị hủy)
		/// 4. Còn lại → Plan không thay đổi (giữ status cũ)
		/// </summary>
		private string DetermineCustomerTreatmentPlanStatus(List<CustomerTreatmentSessionEntity> sessions, string currentPlanStatus)
		{
			if (sessions == null || sessions.Count == 0)
			{
				_logger.LogWarning("DETERMINE_PLAN_STATUS: Không có session nào");
				return currentPlanStatus;
			}

			// Kiểm tra nếu có bất kỳ session nào = "DangThucHien" → Plan = "DangThucHien"
			if (sessions.Any(s => s.Status == "DangThucHien"))
			{
				_logger.LogInformation("DETERMINE_PLAN_STATUS: Có session đang thực hiện - Status: DangThucHien");
				return "DangThucHien";
			}

			// Kiểm tra nếu tất cả session = "HoanThanh" → Plan = "HoanThanh"
			if (sessions.All(s => s.Status == "HoanThanh"))
			{
				_logger.LogInformation("DETERMINE_PLAN_STATUS: Tất cả sessions hoàn thành - Status: HoanThanh");
				return "HoanThanh";
			}

			// Kiểm tra nếu tất cả session = "KhachHuy" → Plan = "KhachHuy" (chỉ khi TẤT CẢ bị hủy)
			if (sessions.All(s => s.Status == "KhachHuy"))
			{
				_logger.LogInformation("DETERMINE_PLAN_STATUS: Tất cả sessions bị hủy - Status: KhachHuy");
				return "KhachHuy";
			}

			// Còn lại → Plan không thay đổi, giữ status cũ
			_logger.LogInformation("DETERMINE_PLAN_STATUS: Trạng thái không thay đổi - Giữ status cũ: {Status}", currentPlanStatus);
			return currentPlanStatus;
		}

		/// <summary>
		/// Mapping từ Appointment Status sang CustomerTreatmentSession Status
		/// </summary>
		private string MapAppointmentStatusToSessionStatus(int appointmentStatus)
		{
			return appointmentStatus switch
			{
				1 => "DaDatLich",      // Booked → Đã đặt lịch
				2 => "DangThucHien",   // InProgress → Đang thực hiện
				3 => "HoanThanh",      // Completed → Hoàn thành
				4 => "KhachHuy",       // Cancelled → Khách hủy
				_ => "DaDatLich"       // Default → Đã đặt lịch
			};
		}

		/// <summary>
		/// Lấy tên trạng thái appointment từ enum value
		/// </summary>
		private string GetAppointmentStatusName(int statusCode)
		{
			return statusCode switch
			{
				1 => "Booked",
				2 => "InProgress",
				3 => "Completed",
				4 => "Cancelled",
				_ => "Unknown"
			};
		}
	}
}