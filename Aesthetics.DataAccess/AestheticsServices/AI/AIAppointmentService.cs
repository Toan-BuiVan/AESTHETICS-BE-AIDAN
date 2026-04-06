using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.AI;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Enum;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices.AI
{
	public class AIAppointmentService : IAIAppointmentService
	{
		private readonly ILogger<AIAppointmentService> _logger;
		private readonly IAppointmentRepositoty _appointmentRepository;
		private readonly IStaffRepository _staffRepository;
		private readonly IServiceRepository _serviceRepository;
		private readonly IAppointmentTimeLockRepository _appointmentTimeLockRepository;
		private readonly IClinicStaffRepository _clinicStaffRepository;
		private readonly ITreatmentPlanRepository _treatmentPlanRepository;
		private readonly ITreatmentSessionRepository _treatmentSessionRepository; 
		private readonly ICustomerTreatmentPlansRepository _customerTreatmentPlansRepository;
		private readonly ICustomerTreatmentSessionsRepository _customerTreatmentSessionsRepository;
		private readonly IAppointmentAssignmentRepository _appointmentAssignmentRepository;
		private readonly IAppointmentService _appointmentService;
		private readonly ICustomerRepository _customerRepository;
		private readonly IInvoiceRepository _invoiceRepository; 
		private readonly IInvoiceDetailsRepository _invoiceDetailsRepository;
		private readonly ICustomerTreatmentSessionsService _customerTreatmentSessionsService;
		private readonly IClinicRepository _clinicRepository;

		private const int	LUNCH_BREAK_START = 12;
		private const int LUNCH_BREAK_END = 13;
		private const int WORKING_HOURS_START = 8;
		private const int WORKING_HOURS_END = 17;
		private const int APPOINTMENT_SLOT_MINUTES = 30;

		public AIAppointmentService(
			ILogger<AIAppointmentService> logger,
			IAppointmentRepositoty appointmentRepository,
			IStaffRepository staffRepository,
			IServiceRepository serviceRepository,
			IAppointmentTimeLockRepository appointmentTimeLockRepository,
			IClinicStaffRepository clinicStaffRepository,
			ITreatmentPlanRepository treatmentPlanRepository,
			ITreatmentSessionRepository treatmentSessionRepository, 
			ICustomerTreatmentPlansRepository customerTreatmentPlansRepository,
			ICustomerTreatmentSessionsRepository customerTreatmentSessionsRepository,
			IAppointmentService appointmentService,
			IAppointmentAssignmentRepository appointmentAssignmentRepository,
			IInvoiceRepository invoiceRepository, 
			IInvoiceDetailsRepository invoiceDetailsRepository,
			ICustomerRepository customerRepository,
			ICustomerTreatmentSessionsService customerTreatmentSessionsService,
			IClinicRepository clinicRepository) 
		{
			_logger = logger;
			_appointmentRepository = appointmentRepository;
			_staffRepository = staffRepository;
			_serviceRepository = serviceRepository;
			_appointmentTimeLockRepository = appointmentTimeLockRepository;
			_clinicStaffRepository = clinicStaffRepository;
			_treatmentPlanRepository = treatmentPlanRepository;
			_treatmentSessionRepository = treatmentSessionRepository; 
			_customerTreatmentPlansRepository = customerTreatmentPlansRepository;
			_customerTreatmentSessionsRepository = customerTreatmentSessionsRepository;
			_appointmentService = appointmentService;
			_appointmentAssignmentRepository = appointmentAssignmentRepository;
			_invoiceRepository = invoiceRepository; 
			_invoiceDetailsRepository = invoiceDetailsRepository;
			_customerRepository = customerRepository;
			_customerTreatmentSessionsService = customerTreatmentSessionsService;
			_clinicRepository = clinicRepository;
		}

		/// <summary>Bài 1-2: Lấy slot trống của bác sĩ trong một ngày</summary>
		public async Task<AIAvailableSlotsResponse> GetDoctorAvailableSlotsAsync(int staffId, DateTime date, int? serviceId = null)
		{
			try
			{
				_logger.LogInformation("=== GET_DOCTOR_AVAILABLE_SLOTS START ===");
				_logger.LogInformation("StaffId={StaffId}, Date={Date}, ServiceId={ServiceId}", staffId, date.Date.ToString("yyyy-MM-dd"), serviceId);

				var response = new AIAvailableSlotsResponse { AvailableSlots = new List<AIAvailableSlot>() };

				// ✅ STEP 1: Kiểm tra bác sĩ tồn tại
				var doctor = await _staffRepository.GetById(staffId);
				if (doctor == null || doctor.DeleteStatus || doctor.IsDoctor != true)
				{
					response.Message = "Bác sĩ không tồn tại hoặc không hoạt động";
					response.Success = false;
					return response;
				}

				response.DoctorName = doctor.FullName;
				response.DoctorId = doctor.Id;
				response.TreatmentPlans = new List<AITreatmentPlanInfo>();

				// ✅ STEP 1.5: Lấy danh sách liệu trình
				// Nếu có serviceId → lấy TreatmentPlans của service đó
				// Nếu không → lấy tất cả TreatmentPlans
				if (serviceId.HasValue)
				{
					var service = await _serviceRepository.GetById(serviceId.Value);
					if (service != null && !service.DeleteStatus)
					{
						var serviceTreatmentPlans = await _treatmentPlanRepository.FindByPredicate(x =>
							x.ServiceId == serviceId.Value &&
							!x.DeleteStatus);

						response.TreatmentPlans = serviceTreatmentPlans.Select(t => new AITreatmentPlanInfo
						{
							Id = t.Id,
							Name = t.PlanName,
							Description = t.Description,
							Price = t.Price
						}).ToList();

						_logger.LogInformation("Found {Count} treatment plans for service {ServiceId}",
							serviceTreatmentPlans.Count(), serviceId.Value);
					}
				}
				else
				{
					// Lấy tất cả liệu trình nếu không chỉ định service
					var allTreatmentPlans = await _treatmentPlanRepository.FindByPredicate(x =>
						!x.DeleteStatus);

					response.TreatmentPlans = allTreatmentPlans.Select(t => new AITreatmentPlanInfo
					{
						Id = t.Id,
						Name = t.PlanName,
						Description = t.Description,
						Price = t.Price
					}).ToList();

					_logger.LogInformation("Found {Count} total treatment plans",
						allTreatmentPlans.Count());
				}

				// ✅ STEP 2: Lấy ClinicId từ ClinicStaff
				var clinicStaff = await _clinicStaffRepository.FindByPredicate(x =>
					x.StaffId == staffId &&
					!x.DeleteStatus);

				if (!clinicStaff.Any())
				{
					response.Message = "Bác sĩ không được gán cho phòng khám nào";
					response.Success = false;
					return response;
				}

				var clinicIds = clinicStaff.Select(x => x.ClinicId).Distinct().ToList();
				_logger.LogInformation("Found {ClinicCount} clinics: {ClinicIds}",
					clinicIds.Count, string.Join(",", clinicIds));

				// ✅ STEP 3: Lấy tất cả lịch hẹn của bác sĩ trong ngày
				_logger.LogInformation("--- Loading Appointments ---");
				var bookedAppointments = await _appointmentRepository.FindByPredicate(x =>
					x.StaffId == staffId &&
					x.StartTime!.Value.Date == date.Date &&
					x.Status != 4 &&
					!x.DeleteStatus);

				_logger.LogInformation("Found {BookedCount} booked appointments", bookedAppointments?.Count ?? 0);

				// ✅ STEP 4: Lấy time locks theo ClinicId
				_logger.LogInformation("--- Loading Time Locks ---");
				var timeLocks = await _appointmentTimeLockRepository.FindByPredicate(x =>
					clinicIds.Contains(x.ClinicId!.Value) &&
					x.StartTime!.Value.Date == date.Date &&
					!x.DeleteStatus);

				_logger.LogInformation("Found {LockCount} time locks", timeLocks?.Count ?? 0);

				// ✅ FIX: Handle null collections
				var appointments = bookedAppointments?.ToList() ?? new List<AppointmentEntity>();
				var locks = timeLocks?.ToList() ?? new List<AppointmentTimeLockEntity>();

				_logger.LogInformation("Before GenerateAvailableSlotsAsync: Appointments={Count}, TimeLocks={Count}",
					appointments.Count, locks.Count);

				// ✅ STEP 5: Tạo danh sách slot trống
				var availableSlots = await GenerateAvailableSlotsAsync(date, appointments, locks);

				response.AvailableSlots = availableSlots;
				response.Success = true;
				response.Message = $"Tìm thấy {availableSlots.Count} slot trống";

				_logger.LogInformation("=== GET_DOCTOR_AVAILABLE_SLOTS END: {SlotCount} slots ===", availableSlots.Count);

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_DOCTOR_AVAILABLE_SLOTS_ERROR");
				return new AIAvailableSlotsResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}",
					AvailableSlots = new List<AIAvailableSlot>()
				};
			}
		}

		/// <summary>Bài 1: Lấy slot trống của bác sĩ cho liệu trình cụ thể</summary>
		public async Task<AIAvailableSlotsResponse> GetDoctorAvailableSlotsForTreatmentPlanAsync(int staffId, int treatmentPlanId, DateTime date)
		{
			try
			{
				_logger.LogInformation("GET_DOCTOR_AVAILABLE_SLOTS_FOR_PLAN: staffId={StaffId}, treatmentPlanId={PlanId}, date={Date}",
					staffId, treatmentPlanId, date.Date);

				var response = new AIAvailableSlotsResponse { AvailableSlots = new List<AIAvailableSlot>() };

				// ✅ STEP 1: Kiểm tra bác sĩ tồn tại
				var doctor = await _staffRepository.GetById(staffId);
				if (doctor == null || doctor.DeleteStatus || doctor.IsDoctor != true)
				{
					response.Message = "Bác sĩ không tồn tại";
					response.Success = false;
					return response;
				}

				response.DoctorName = doctor.FullName;
				response.DoctorId = doctor.Id;

				// ✅ STEP 2: Kiểm tra liệu trình tồn tại
				var treatmentPlan = await _treatmentPlanRepository.GetById(treatmentPlanId);
				if (treatmentPlan == null || treatmentPlan.DeleteStatus)
				{
					response.Message = "Liệu trình không tồn tại";
					response.Success = false;
					return response;
				}

				// ✅ STEP 2.5: Lấy tất cả liệu trình của service (để hiển thị danh sách)
				if (treatmentPlan.ServiceId.HasValue)
				{
					var allTreatmentPlans = await _treatmentPlanRepository.FindByPredicate(x =>
						x.ServiceId == treatmentPlan.ServiceId &&
						!x.DeleteStatus);

					response.TreatmentPlans = allTreatmentPlans.Select(t => new AITreatmentPlanInfo
					{
						Id = t.Id,
						Name = t.PlanName,
						Description = t.Description,
						Price = t.Price
					}).ToList();

					_logger.LogInformation("Found {Count} treatment plans for service {ServiceId}",
						allTreatmentPlans.Count(), treatmentPlan.ServiceId);
				}
				else
				{
					response.TreatmentPlans = new List<AITreatmentPlanInfo>();
				}

				// ✅ STEP 3: Lấy ClinicId từ ClinicStaff
				var clinicStaff = await _clinicStaffRepository.FindByPredicate(x =>
					x.StaffId == staffId &&
					!x.DeleteStatus);

				if (!clinicStaff.Any())
				{
					response.Message = "Bác sĩ không được gán cho phòng khám nào";
					response.Success = false;
					return response;
				}

				var clinicIds = clinicStaff.Select(x => x.ClinicId).Distinct().ToList();

				// ✅ STEP 4: Lấy lịch hẹn của bác sĩ trong ngày
				// Filter: StaffId + Date + Status != 4 (Cancelled) + Not Deleted
				var bookedAppointments = await _appointmentRepository.FindByPredicate(x =>
					x.StaffId == staffId &&
					x.StartTime!.Value.Date == date.Date &&
					x.Status != 4 && // ✅ Status 4 = Cancelled
					!x.DeleteStatus);

				_logger.LogInformation("Found {BookedCount} booked appointments for doctor {StaffId} on {Date}",
					bookedAppointments.Count, staffId, date.Date);

				// ✅ STEP 5: Lấy time locks theo ClinicId
				var timeLocks = await _appointmentTimeLockRepository.FindByPredicate(x =>
					clinicIds.Contains(x.ClinicId!.Value) &&
					x.StartTime!.Value.Date == date.Date &&
					!x.DeleteStatus);

				_logger.LogInformation("Found {LockCount} time locks for clinics {ClinicIds} on {Date}",
					timeLocks.Count, string.Join(",", clinicIds), date.Date);

				var availableSlots = await GenerateAvailableSlotsAsync(date, bookedAppointments.ToList(), timeLocks.ToList());
				response.AvailableSlots = availableSlots;
				response.Success = true;
				response.Message = $"Tìm thấy {availableSlots.Count} slot trống cho liệu trình {treatmentPlan.PlanName}";

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_DOCTOR_AVAILABLE_SLOTS_FOR_PLAN_ERROR: Exception occurred");
				return new AIAvailableSlotsResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}",
					AvailableSlots = new List<AIAvailableSlot>()
				};
			}
		}

		/// <summary>Bài 3: Lấy danh sách bác sĩ của một dịch vụ</summary>
		public async Task<AIServiceDoctorsResponse> GetDoctorsForServiceAsync(int serviceId)
		{
			try
			{
				_logger.LogInformation("GET_DOCTORS_FOR_SERVICE: serviceId={ServiceId}", serviceId);

				var response = new AIServiceDoctorsResponse { Doctors = new List<AIServiceDoctor>() };

				// Kiểm tra dịch vụ
				var service = await _serviceRepository.GetById(serviceId);
				if (service == null || service.DeleteStatus)
				{
					response.Message = "Dịch vụ không tồn tại";
					response.Success = false;
					return response;
				}

				response.ServiceName = service.ServiceName;
				response.ServiceDescription = service.Description;

				// 🆕 STEP 3: Vào Clinic - lấy clinicId dựa trên ServiceTypeId
				var clinics = await _clinicRepository.FindByPredicate(x =>
					x.ServiceTypeId == service.ServiceTypeId &&
					!x.DeleteStatus);

				if (!clinics.Any())
				{
					response.Message = $"Không tìm thấy phòng khám nào cung cấp dịch vụ '{service.ServiceName}'";
					response.Success = true;
					response.Doctors = new List<AIServiceDoctor>();
					return response;
				}

				var clinicIds = clinics.Select(x => x.Id).ToList();
				_logger.LogInformation("✓ Found {Count} clinics for ServiceTypeId {ServiceTypeId}: {ClinicIds}",
					clinicIds.Count, service.ServiceTypeId, string.Join(",", clinicIds));

				var clinicStaffs = await _clinicStaffRepository.FindByPredicate(x =>
					clinicIds.Contains(x.ClinicId ?? 0) &&
					!x.DeleteStatus);

				if (!clinicStaffs.Any())
				{
					response.Message = $"Không tìm thấy bác sĩ nào ở phòng khám cung cấp dịch vụ '{service.ServiceName}'";
					response.Success = true;
					response.Doctors = new List<AIServiceDoctor>();
					return response;
				}

				var staffIds = clinicStaffs
					.Where(x => x.StaffId.HasValue)
					.Select(x => x.StaffId.Value)
					.Distinct()
					.ToList();

				_logger.LogInformation("✓ Found {Count} doctors in clinics: {StaffIds}",
					staffIds.Count, string.Join(",", staffIds));

				// Lấy thông tin bác sĩ
				var doctors = new List<AIServiceDoctor>();
				foreach (var staffId in staffIds)
				{
					try
					{
						var staff = await _staffRepository.GetById(staffId);
						if (staff != null && staff.IsDoctor == true && !staff.DeleteStatus)
						{
							// ✅ Đếm số lịch hẹn của bác sĩ CHO DỊCH VỤ NÀY
							var appointmentCount = (await _appointmentRepository.FindByPredicate(x =>
								x.StaffId == staffId &&
								x.ServiceId == serviceId &&
								x.Status != (int)AppointmentStatus.Cancelled &&
								!x.DeleteStatus)).Count();

							_logger.LogInformation("Doctor {StaffId} ({Name}): {Count} appointments for service {ServiceId}",
								staffId, staff.FullName, appointmentCount, serviceId);

							doctors.Add(new AIServiceDoctor
							{
								StaffId = staff.Id,
								Name = staff.FullName,
								Specialization = staff.Specialization,
								Experience = staff.ExperienceYears ?? 0,
								Degree = staff.Degree,
								Rating = CalculateDoctorRating(appointmentCount),
								AppointmentCount = appointmentCount
							});
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error processing doctor {StaffId}", staffId);
					}
				}

				response.Doctors = doctors.OrderByDescending(x => x.AppointmentCount).ToList();
				response.Success = true;
				response.Message = $"Tìm thấy {doctors.Count} bác sĩ cho dịch vụ {service.ServiceName}";

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_DOCTORS_FOR_SERVICE_ERROR: Exception occurred");
				return new AIServiceDoctorsResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}",
					Doctors = new List<AIServiceDoctor>()
				};
			}
		}

		/// <summary>Bài 7-8: Đặt lịch khám</summary>
		public async Task<AIBookAppointmentResponse> BookAppointmentAsync(
			int customerId,
			int staffId,
			int serviceId,
			DateTime appointmentDate,
			string appointmentTime,
			int? treatmentPlanId = null,
			int? sessionNumber = null)
		{
			try
			{
				_logger.LogInformation("BOOK_APPOINTMENT: customerId={CustomerId}, staffId={StaffId}, serviceId={ServiceId}, date={Date}, time={Time}, treatmentPlanId={TreatmentPlanId}, sessionNumber={SessionNumber}",
					customerId, staffId, serviceId, appointmentDate.Date, appointmentTime, treatmentPlanId, sessionNumber);

				var response = new AIBookAppointmentResponse();

				// ✅ STEP 1: Parse appointment time
				if (!DateTime.TryParse($"{appointmentDate:yyyy-MM-dd} {appointmentTime}", out var startTime))
				{
					response.Success = false;
					response.Message = "Thời gian không hợp lệ";
					return response;
				}

				// ✅ STEP 2: Kiểm tra khách hàng, bác sĩ, dịch vụ
				var customer = await _customerRepository.GetById(customerId);
				if (customer == null || customer.DeleteStatus)
				{
					response.Success = false;
					response.Message = "Khách hàng không tồn tại";
					return response;
				}

				var staff = await _staffRepository.GetById(staffId);
				if (staff == null || staff.DeleteStatus || staff.IsDoctor != true)
				{
					response.Success = false;
					response.Message = "Bác sĩ không tồn tại";
					return response;
				}

				var service = await _serviceRepository.GetById(serviceId);
				if (service == null || service.DeleteStatus)
				{
					response.Success = false;
					response.Message = "Dịch vụ không tồn tại";
					return response;
				}

				_logger.LogInformation("✓ Service found: {ServiceName}", service.ServiceName);

				// ✅ STEP 3: Kiểm tra lịch trống
				var existingAppointments = await _appointmentRepository.FindByPredicate(x =>
					x.StaffId == staffId &&
					x.StartTime!.Value.Date == appointmentDate.Date &&
					x.Status != (int)AppointmentStatus.Cancelled &&
					!x.DeleteStatus);

				bool hasConflict = existingAppointments.Any(a =>
					a.StartTime!.Value.Hour == startTime.Hour && a.StartTime.Value.Minute == startTime.Minute);

				if (hasConflict)
				{
					response.Success = false;
					response.Message = "Bác sĩ đã có lịch vào thời gian này";
					return response;
				}

				// ✅ STEP 4: Nếu có sessionNumber → tìm/tạo customer treatment plan/session
				int? customerTreatmentSessionId = null;

				if (sessionNumber.HasValue)
				{
					_logger.LogInformation("🔍 Processing treatment session: sessionNumber={SessionNumber}", sessionNumber);

					// 4a: Lấy TreatmentPlans của service
					var treatmentPlans = await _treatmentPlanRepository.FindByPredicate(x =>
						x.ServiceId == serviceId &&
						!x.DeleteStatus);

					if (!treatmentPlans.Any())
					{
						response.Success = false;
						response.Message = $"Không tìm thấy liệu trình nào cho dịch vụ '{service.ServiceName}'";
						_logger.LogError("No treatment plans found for serviceId={ServiceId}", serviceId);
						return response;
					}

					// Nếu treatmentPlanId được truyền, dùng cái đó. Nếu không, lấy treatment plan đầu tiên
					TreatmentPlanEntity selectedTreatmentPlan = null;

					if (treatmentPlanId.HasValue)
					{
						selectedTreatmentPlan = treatmentPlans.FirstOrDefault(x => x.Id == treatmentPlanId.Value);
						if (selectedTreatmentPlan == null)
						{
							response.Success = false;
							response.Message = $"Liệu trình ID {treatmentPlanId} không thuộc dịch vụ này";
							return response;
						}
					}
					else
					{
						// Lấy treatment plan đầu tiên hoặc mặc định
						selectedTreatmentPlan = treatmentPlans.First();
						_logger.LogInformation("No treatmentPlanId specified, using default: {PlanName}", selectedTreatmentPlan.PlanName);
					}

					_logger.LogInformation("✓ Treatment plan selected: {PlanName} (ID={PlanId})", selectedTreatmentPlan.PlanName, selectedTreatmentPlan.Id);

					// 4b: Lấy TreatmentSessions của treatment plan
					var treatmentSessions = await _treatmentSessionRepository.FindByPredicate(x =>
						x.TreatmentPlanId == selectedTreatmentPlan.Id &&
						!x.DeleteStatus);

					if (!treatmentSessions.Any())
					{
						response.Success = false;
						response.Message = $"Liệu trình '{selectedTreatmentPlan.PlanName}' không có buổi nào";
						_logger.LogError("No treatment sessions found for planId={PlanId}", selectedTreatmentPlan.Id);
						return response;
					}

					// Lấy TreatmentSession theo sessionNumber
					var treatmentSession = treatmentSessions.FirstOrDefault(x => x.SessionNumber == sessionNumber);
					if (treatmentSession == null)
					{
						response.Success = false;
						response.Message = $"Buổi thứ {sessionNumber} không tồn tại trong liệu trình '{selectedTreatmentPlan.PlanName}'";
						_logger.LogError("Treatment session #{SessionNumber} not found", sessionNumber);
						return response;
					}

					_logger.LogInformation("✓ Treatment session found: buổi {SessionNumber}, ID={SessionId}", sessionNumber, treatmentSession.Id);

					// 4c: TÌM hoặc TẠO CustomerTreatmentPlan
					var existingCustomerTreatmentPlans = await _customerTreatmentPlansRepository.FindByPredicate(x =>
						x.CustomerId == customerId &&
						x.TreatmentPlanId == selectedTreatmentPlan.Id &&
						x.Status == "ChoDatLich" &&
						!x.DeleteStatus);

					CustomerTreatmentPlanEntity customerTreatmentPlan;

					if (existingCustomerTreatmentPlans.Any())
					{
						customerTreatmentPlan = existingCustomerTreatmentPlans.First();
						_logger.LogInformation("✓ Existing CustomerTreatmentPlan found: ID={Id}", customerTreatmentPlan.Id);
					}
					else
					{
						customerTreatmentPlan = new CustomerTreatmentPlanEntity
						{
							CustomerId = customerId,
							TreatmentPlanId = selectedTreatmentPlan.Id,
							Status = "ChoDatLich",
							DeleteStatus = false
						};

						var created = await _customerTreatmentPlansRepository.CreateEntity(customerTreatmentPlan);
						if (!created)
						{
							response.Success = false;
							response.Message = "Không thể tạo liệu trình cho khách hàng";
							_logger.LogError("Failed to create CustomerTreatmentPlan");
							return response;
						}

						_logger.LogInformation("✓ New CustomerTreatmentPlan created: ID={Id}", customerTreatmentPlan.Id);
					}

					// 4d: LẤY CustomerTreatmentSessions từ CustomerTreatmentPlan
					var existingCustomerTreatmentSessions = await _customerTreatmentSessionsRepository.FindByPredicate(x =>
						x.CustomerTreatmentPlanId == customerTreatmentPlan.Id &&
						!x.DeleteStatus);

					_logger.LogInformation("📋 Found {Count} CustomerTreatmentSessions for customerTreatmentPlanId={PlanId}", 
						existingCustomerTreatmentSessions.Count(), customerTreatmentPlan.Id);

					// ✅ Lấy TreatmentSession để so sánh SessionNumber
					// TreatmentSession có SessionNumber, không phải CustomerTreatmentSession
					CustomerTreatmentSessionEntity customerTreatmentSession = null;

					foreach (var cts in existingCustomerTreatmentSessions)
					{
						// Lấy TreatmentSession qua navigation property hoặc query
						var ts = await _treatmentSessionRepository.GetById(cts.TreatmentSessionId ?? 0);
						if (ts != null && ts.SessionNumber == sessionNumber )
						{
							customerTreatmentSession = cts;
							_logger.LogInformation("✓ Found CustomerTreatmentSession for SessionNumber={SessionNumber}: ID={CtsId}", 
								sessionNumber, cts.Id);
							break;
						}
					}

					// Nếu không tìm thấy, tạo mới
					if (customerTreatmentSession == null)
					{
						// SessionNumber được truyền vào từ user, cần tìm TreatmentSession tương ứng
						var matchingTreatmentSession = treatmentSessions.FirstOrDefault(x => x.SessionNumber == sessionNumber);
						if (matchingTreatmentSession == null)
						{
							response.Success = false;
							response.Message = $"Buổi thứ {sessionNumber} không tồn tại";
							_logger.LogError("Treatment session with SessionNumber={SessionNumber} not found", sessionNumber);
							return response;
						}

						customerTreatmentSession = new CustomerTreatmentSessionEntity
						{
							CustomerTreatmentPlanId = customerTreatmentPlan.Id,
							TreatmentSessionId = matchingTreatmentSession.Id,
							Status = "ChoDatLich",
							DeleteStatus = false
						};

						var created = await _customerTreatmentSessionsRepository.CreateEntity(customerTreatmentSession);
						if (!created)
						{
							response.Success = false;
							response.Message = "Không thể tạo buổi điều trị cho khách hàng";
							_logger.LogError("Failed to create CustomerTreatmentSession");
							return response;
						}

						_logger.LogInformation("✓ New CustomerTreatmentSession created: ID={Id}, TreatmentSessionId={TsId}, buổi {SessionNumber}", 
							customerTreatmentSession.Id, matchingTreatmentSession.Id, sessionNumber);
					}
					else
					{
						_logger.LogInformation("✓ Using existing CustomerTreatmentSession: ID={Id}, buổi {SessionNumber}", 
							customerTreatmentSession.Id, sessionNumber);
					}

					customerTreatmentSessionId = customerTreatmentSession.Id;
				}

				// ✅ STEP 5: Tạo Appointment
				_logger.LogInformation("📅 Creating appointment: startTime={StartTime}, customerTreatmentSessionId={SessionId}",
					startTime, customerTreatmentSessionId);

				var createAppointmentRequest = new CreateAppointment
				{
					CustomerId = customerId,
					StaffId = staffId,
					ServiceId = serviceId,
					CustomerTreatmentSessionId = customerTreatmentSessionId,
					CustomerTreatmentPlanId = treatmentPlanId,
					SessionNumber = sessionNumber,
					StartTime = startTime,
					PaidAmount = 0,
					PaymentMethod = "TienMat"
				};

				var isBooked = await _appointmentService.create(createAppointmentRequest);
				if (!isBooked)
				{
					response.Success = false;
					response.Message = "Không thể đặt lịch. Vui lòng kiểm tra thông tin";
					_logger.LogError("❌ Failed to create appointment");
					return response;
				}

				_logger.LogInformation("✓ Appointment created successfully");

				// ✅ STEP 6: Lấy thông tin Appointment vừa tạo
				var appointments = await _appointmentRepository.FindByPredicate(x =>
					x.CustomerId == customerId &&
					x.StaffId == staffId &&
					x.StartTime == startTime &&
					!x.DeleteStatus);

				var createdAppointment = appointments.FirstOrDefault();
				if (createdAppointment != null)
				{
					_logger.LogInformation("✓ Found created appointment: ID={AppointmentId}", createdAppointment.Id);
					// ✅ STEP 7 (cũ): Tạo AppointmentAssignment
					_logger.LogInformation("📌 Creating AppointmentAssignment for staffId={StaffId}", staffId);

					var appointmentAssignment = new AppointmentAssignmentEntity
					{
						AppointmentId = createdAppointment.Id,
						StaffId = staffId,
						AssignedDate = DateTime.UtcNow,
						Status = (int)AppointmentStatus.Booked,
						DeleteStatus = false
					};

					var assignmentCreated = await _appointmentAssignmentRepository.CreateEntity(appointmentAssignment);
					if (assignmentCreated)
					{
						_logger.LogInformation("✓ AppointmentAssignment created: ID={AssignmentId}", appointmentAssignment.Id);
					}
					else
					{
						_logger.LogWarning("⚠ Failed to create AppointmentAssignment, but appointment still created");
					}

					// ✅ STEP 8: CẬP NHẬT STATUS CUSTOMERTREATMENTSESSION VIA SERVICE
					if (customerTreatmentSessionId.HasValue)
					{
						_logger.LogInformation("📌 Updating CustomerTreatmentSession status via Service: SessionId={SessionId}",
							customerTreatmentSessionId);

						var updateCtsRequest = new UpdateCustomerTreatmentSessions
						{
							Id = customerTreatmentSessionId.Value,
							Status = "DaDatLich"
						};

						var ctsStatusUpdated = await _customerTreatmentSessionsService.update(updateCtsRequest);
						if (ctsStatusUpdated)
						{
							_logger.LogInformation("✓ CTS Status updated successfully to 'DaDatLich' via Service");
						}
						else
						{
							_logger.LogWarning("⚠ Failed to update CTS status via Service, but appointment still created");
						}
					}

					// ✅ STEP 9: Trả về response
					response.Success = true;
					response.AppointmentId = createdAppointment.Id;

					var message = sessionNumber.HasValue
						? $"✅ Đặt lịch buổi {sessionNumber} thành công vào lúc {startTime:HH:mm} ngày {startTime:dd/MM/yyyy}"
						: $"✅ Đặt lịch thành công vào lúc {startTime:HH:mm} ngày {startTime:dd/MM/yyyy}";

					response.Message = message;
					_logger.LogInformation("✓ Booking completed successfully: {Message}", message);
				}
				else
				{
					response.Success = false;
					response.Message = "Không thể lấy thông tin lịch hẹn vừa tạo";
					_logger.LogError("❌ Could not retrieve created appointment");
				}

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "BOOK_APPOINTMENT_ERROR: Exception occurred - {Message}", ex.Message);
				return new AIBookAppointmentResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}"
				};
			}
		}

		/// <summary>Bài 9-11: Hủy lịch hẹn</summary>
		public async Task<AICancelAppointmentResponse> CancelAppointmentAsync(int customerId, int staffId, DateTime? appointmentDate = null, int? serviceId = null)
		{
			try
			{
				_logger.LogInformation("CANCEL_APPOINTMENT: customerId={CustomerId}, staffId={StaffId}, date={Date}, serviceId={ServiceId}",
					customerId, staffId, appointmentDate?.Date, serviceId);

				var response = new AICancelAppointmentResponse();

				// STEP 1: Kiểm tra bác sĩ tồn tại
				var staff = await _staffRepository.GetById(staffId);
				if (staff == null || staff.DeleteStatus || staff.IsDoctor != true)
				{
					response.Success = false;
					response.Message = "Bác sĩ không tồn tại hoặc không hoạt động";
					_logger.LogError("Staff not found or not a doctor: staffId={StaffId}", staffId);
					return response;
				}

				// STEP 2: Kiểm tra khách hàng tồn tại
				var customer = await _customerRepository.GetById(customerId);
				if (customer == null || customer.DeleteStatus)
				{
					response.Success = false;
					response.Message = "Khách hàng không tồn tại";
					_logger.LogError("Customer not found: customerId={CustomerId}", customerId);
					return response;
				}

				_logger.LogInformation("✓ Customer={CustomerName}, Doctor={DoctorName} verified",
					customer.FullName, staff.FullName);

				// STEP 3: Tìm lịch hẹn cần hủy
				var appointments = await _appointmentRepository.FindByPredicate(x =>
					x.CustomerId == customerId &&
					x.StaffId == staffId &&
					x.Status != (int)AppointmentStatus.Cancelled &&
					!x.DeleteStatus);

				// STEP 4: Lọc theo ngày nếu có
				if (appointmentDate.HasValue)
				{
					appointments = appointments
						.Where(x => x.StartTime!.Value.Date == appointmentDate.Value.Date)
						.ToList();
					_logger.LogInformation("After date filter: {Count} appointments on {Date}",
						appointments.Count(), appointmentDate.Value.Date.ToString("dd-MM-yyyy"));
				}

				// STEP 5: Lọc theo dịch vụ nếu có
				if (serviceId.HasValue)
				{
					appointments = appointments
						.Where(x => x.ServiceId == serviceId)
						.ToList();
					_logger.LogInformation("After service filter: {Count} appointments", appointments.Count());
				}

				// STEP 6: Kiểm tra có lịch hẹn để hủy không
				if (!appointments.Any())
				{
					response.Success = false;
					response.Message = $"Không tìm thấy lịch hẹn của khách hàng với bác sĩ {staff.FullName} để hủy" +
						(appointmentDate.HasValue ? $" vào ngày {appointmentDate:dd-MM-yyyy}" : "") +
						(serviceId.HasValue ? $" cho dịch vụ này" : "");
					_logger.LogWarning("No appointments found to cancel");
					return response;
				}

				// STEP 7: Hủy tất cả lịch hẹn tìm được
				_logger.LogInformation("📅 Cancelling {Count} appointments", appointments.Count());

				int cancelledCount = 0;
				int cancelledAssignmentCount = 0;
				var cancelledDetails = new List<string>();
				var customerTreatmentSessionIds = new HashSet<int>(); 

				foreach (var appointment in appointments)
				{
					try
					{
						appointment.Status = (int)AppointmentStatus.Cancelled;

						var updated = await _appointmentRepository.UpdateEntity(appointment);

						if (updated)
						{
							cancelledCount++;
							cancelledDetails.Add($"{appointment.StartTime:dd/MM/yyyy HH:mm}");
							_logger.LogInformation("✓ Appointment cancelled: ID={AppointmentId}, StartTime={StartTime}",
								appointment.Id, appointment.StartTime);

							// ✅ STEP 7.1: Update status của AppointmentAssignments liên quan
							var assignments = await _appointmentAssignmentRepository.FindByPredicate(x =>
								x.AppointmentId == appointment.Id &&
								!x.DeleteStatus);

							foreach (var assignment in assignments)
							{
								try
								{
									// Cập nhật status thành Cancelled (4)
									assignment.Status = (int)AppointmentStatus.Cancelled;

									var assignmentUpdated = await _appointmentAssignmentRepository.UpdateEntity(assignment);
									if (assignmentUpdated)
									{
										cancelledAssignmentCount++;
										_logger.LogInformation("✓ AppointmentAssignment status updated: ID={AssignmentId}, Status=Cancelled",
											assignment.Id);
									}
									else
									{
										_logger.LogWarning("⚠ Failed to update AppointmentAssignment status: ID={AssignmentId}",
											assignment.Id);
									}
								}
								catch (Exception ex)
								{
									_logger.LogError(ex, "Error updating AppointmentAssignment status: ID={AssignmentId}",
										assignment.Id);
								}
							}

							// ✅ STEP 7.2: Lưu SessionId để update status sau
							if (appointment.CustomerTreatmentSessionId.HasValue)
							{
								customerTreatmentSessionIds.Add(appointment.CustomerTreatmentSessionId.Value);
							}
						}
						else
						{
							_logger.LogWarning("⚠ Failed to cancel appointment: ID={AppointmentId}", appointment.Id);
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error cancelling appointment: ID={AppointmentId}", appointment.Id);
					}
				}

				// 🆕 STEP 8: Cập nhật status của CustomerTreatmentSession và CustomerTreatmentPlan
				int statusUpdateCount = 0;
				if (customerTreatmentSessionIds.Count > 0)
				{
					_logger.LogInformation("📋 Updating status for {Count} CustomerTreatmentSession(s)", customerTreatmentSessionIds.Count);

					foreach (var sessionId in customerTreatmentSessionIds)
					{
						try
						{
							// Lấy session để update status
							var session = await _customerTreatmentSessionsRepository.GetById(sessionId);
							if (session != null && !session.DeleteStatus)
							{
								// Cập nhật status thành "KhachHuy"
								session.Status = "KhachHuy";
								var statusUpdated = await _customerTreatmentSessionsRepository.UpdateEntity(session);

								if (statusUpdated)
								{
									statusUpdateCount++;
									_logger.LogInformation("✓ Session status updated: SessionId={SessionId}, Status=KhachHuy",
										sessionId);

									// Cập nhật CustomerTreatmentPlan nếu có
									if (session.CustomerTreatmentPlanId.HasValue)
									{
										var plan = await _customerTreatmentPlansRepository.GetById(session.CustomerTreatmentPlanId.Value);
										if (plan != null && !plan.DeleteStatus)
										{
											// Kiểm tra xem tất cả sessions của plan đã bị hủy chưa
											var allSessions = await _customerTreatmentSessionsRepository.FindByPredicate(x =>
												x.CustomerTreatmentPlanId == session.CustomerTreatmentPlanId.Value &&
												!x.DeleteStatus);

											// Nếu TẤT CẢ sessions = "KhachHuy" → Plan = "KhachHuy"
											if (allSessions.All(s => s.Status == "KhachHuy"))
											{
												plan.Status = "KhachHuy";
												var planUpdated = await _customerTreatmentPlansRepository.UpdateEntity(plan);

												if (planUpdated)
												{
													_logger.LogInformation("✓ Plan status updated to KhachHuy: PlanId={PlanId}",
														session.CustomerTreatmentPlanId.Value);
												}
											}
											else
											{
												_logger.LogInformation("ℹ Plan has other active sessions, status not changed: PlanId={PlanId}",
													session.CustomerTreatmentPlanId.Value);
											}
										}
									}
								}
								else
								{
									_logger.LogWarning("⚠ Failed to update session status: SessionId={SessionId}", sessionId);
								}
							}
						}
						catch (Exception ex)
						{
							_logger.LogError(ex, "Error updating session status: SessionId={SessionId}", sessionId);
						}
					}
				}

				// 🆕 STEP 9: Trả về kết quả
				response.Success = cancelledCount > 0;
				response.CancelledCount = cancelledCount;
				response.Message = cancelledCount > 0
					? $"✅ Đã hủy {cancelledCount} lịch hẹn của khách hàng với bác sĩ {staff.FullName}" +
					  (appointmentDate.HasValue ? $" vào ngày {appointmentDate:dd-MM-yyyy}" : "") +
					  (cancelledDetails.Count > 0 ? $": {string.Join(", ", cancelledDetails)}" : "") +
					  (cancelledAssignmentCount > 0 ? $" | Xóa {cancelledAssignmentCount} assignment(s)" : "") +
					  (statusUpdateCount > 0 ? $" | Cập nhật {statusUpdateCount} session(s)" : "")
					: "❌ Không thể hủy lịch hẹn";

				_logger.LogInformation("✓ Cancel operation completed: {Message}", response.Message);

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CANCEL_APPOINTMENT_ERROR: Exception occurred");
				return new AICancelAppointmentResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}",
					CancelledCount = 0
				};
			}
		}

		/// <summary>Tính toán rating của bác sĩ dựa trên số lượng lịch hẹn</summary>
		private decimal CalculateDoctorRating(int appointmentCount)
		{
			// Rating = min(5, appointmentCount / 10)
			// Mỗi 10 lịch hẹn = 1 điểm, tối đa 5 sao
			return Math.Min(5m, appointmentCount / 10m);
		}

		/// <summary>Tạo danh sách slot trống cho một ngày</summary>
		private async Task<List<AIAvailableSlot>> GenerateAvailableSlotsAsync(
			DateTime date,
			List<AppointmentEntity> bookedAppointments,
			List<AppointmentTimeLockEntity> timeLocks)
		{
			var slots = new List<AIAvailableSlot>();
			const int serviceDuration = 60; // ✅ 60 PHÚT

			_logger.LogInformation("=== GENERATE AVAILABLE SLOTS START ===");
			_logger.LogInformation("Date: {Date}, Appointments: {Count}, TimeLocks: {Count}",
				date.Date.ToString("yyyy-MM-dd"), bookedAppointments.Count, timeLocks.Count);

			// ✅ Build busy times
			var busyTimes = new List<(DateTime Start, DateTime End)>();

			// ✅ Thêm appointments
			foreach (var apt in bookedAppointments)
			{
				if (apt.StartTime.HasValue)
				{
					busyTimes.Add((apt.StartTime.Value, apt.StartTime.Value.AddMinutes(serviceDuration)));
					_logger.LogInformation("Appointment: {Start:HH:mm} - {End:HH:mm}",
						apt.StartTime.Value, apt.StartTime.Value.AddMinutes(serviceDuration));
				}
			}

			// ✅ Thêm time locks
			foreach (var tl in timeLocks)
			{
				if (tl.StartTime.HasValue && tl.EndTime.HasValue)
				{
					busyTimes.Add((tl.StartTime.Value, tl.EndTime.Value));
					_logger.LogInformation("TimeLock: {Start:HH:mm} - {End:HH:mm}",
						tl.StartTime.Value, tl.EndTime.Value);
				}
			}

			// ✅ Thêm lunch break
			var lunchStart = date.Date.AddHours(LUNCH_BREAK_START);
			var lunchEnd = date.Date.AddHours(LUNCH_BREAK_END);
			busyTimes.Add((lunchStart, lunchEnd));
			_logger.LogInformation("LunchBreak: {Start:HH:mm} - {End:HH:mm}", lunchStart, lunchEnd);

			// ✅ Tạo slots
			var currentTime = date.Date.AddHours(WORKING_HOURS_START);
			while (currentTime.Hour < WORKING_HOURS_END)
			{
				var slotEnd = currentTime.AddMinutes(APPOINTMENT_SLOT_MINUTES);

				// ✅ Check conflict
				bool hasConflict = busyTimes.Any(busy =>
					currentTime < busy.End && slotEnd > busy.Start);

				if (!hasConflict)
				{
					slots.Add(new AIAvailableSlot
					{
						Date = currentTime.ToString("yyyy-MM-dd"),
						StartTime = currentTime.ToString("HH:mm"),        // ✅ StartTime (30 phút slot)
						EndTime = slotEnd.ToString("HH:mm"),              // ✅ EndTime (30 phút slot)
						SlotDateTime = currentTime
					});
					_logger.LogInformation("✓ Slot: {Start:HH:mm} - {End:HH:mm}",
						currentTime.ToString("HH:mm"), slotEnd.ToString("HH:mm"));
				}
				else
				{
					_logger.LogInformation("✗ Blocked: {Start:HH:mm}", currentTime.ToString("HH:mm"));
				}

				currentTime = currentTime.AddMinutes(APPOINTMENT_SLOT_MINUTES);
			}

			_logger.LogInformation("=== TOTAL SLOTS: {Count} ===", slots.Count);
			return slots.OrderBy(x => x.SlotDateTime).ToList();
		}

		/// <summary>Tìm bác sĩ của dịch vụ và trả về slot trống cho ngày cụ thể</summary>
		public async Task<AIAvailableSlotsForServiceResponse> GetAvailableSlotsForServiceAsync(int serviceId, DateTime date, int? treatmentPlanId = null)
		{
			try
			{
				_logger.LogInformation("GET_AVAILABLE_SLOTS_FOR_SERVICE: serviceId={ServiceId}, date={Date}, treatmentPlanId={TreatmentPlanId}",
					serviceId, date.Date, treatmentPlanId);

				var response = new AIAvailableSlotsForServiceResponse { Doctors = new List<AIAvailableSlotsForServiceDoctor>() };

				// ✅ STEP 1: Kiểm tra dịch vụ
				var service = await _serviceRepository.GetById(serviceId);
				if (service == null || service.DeleteStatus)
				{
					response.Message = "Dịch vụ không tồn tại";
					response.Success = false;
					return response;
				}

				response.ServiceName = service.ServiceName;
				response.ServiceDescription = service.Description;

				// ✅ STEP 2: Lấy danh sách bác sĩ của dịch vụ
				var allClinicStaffs = await _clinicStaffRepository.FindByPredicate(x => !x.DeleteStatus);
				var staffIds = allClinicStaffs.Select(x => x.StaffId).Distinct().ToList();

				var doctors = new List<AIAvailableSlotsForServiceDoctor>();

				foreach (var staffId in staffIds)
				{
					var staff = await _staffRepository.GetById(staffId ?? 0);
					if (staff != null && staff.IsDoctor == true && !staff.DeleteStatus)
					{
						// Kiểm tra bác sĩ có lịch hẹn cho dịch vụ này không
						var appointmentCount = (await _appointmentRepository.FindByPredicate(x =>
							x.StaffId == staffId &&
							x.ServiceId == serviceId &&
							x.Status != (int)AppointmentStatus.Cancelled &&
							!x.DeleteStatus)).Count();

						if (appointmentCount > 0)
						{
							// ✅ STEP 3: Lấy slot trống cho từng bác sĩ
							AIAvailableSlotsResponse slotsResponse;

							if (treatmentPlanId.HasValue)
							{
								slotsResponse = await GetDoctorAvailableSlotsForTreatmentPlanAsync(staffId.Value, treatmentPlanId.Value, date);
							}
							else
							{
								slotsResponse = await GetDoctorAvailableSlotsAsync(staffId.Value, date);
							}

							if (slotsResponse.Success && slotsResponse.AvailableSlots.Any())
							{
								doctors.Add(new AIAvailableSlotsForServiceDoctor
								{
									StaffId = staff.Id,
									Name = staff.FullName,
									Specialization = staff.Specialization,
									Experience = staff.ExperienceYears ?? 0,
									Degree = staff.Degree,
									Rating = CalculateDoctorRating(appointmentCount),
									AvailableSlots = slotsResponse.AvailableSlots
								});
							}
						}
					}
				}

				response.Doctors = doctors.OrderByDescending(x => x.AvailableSlots.Count).ToList();
				response.Success = true;
				response.Message = $"Tìm thấy {doctors.Count} bác sĩ có slot trống cho dịch vụ {service.ServiceName}";

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_AVAILABLE_SLOTS_FOR_SERVICE_ERROR: Exception occurred");
				return new AIAvailableSlotsForServiceResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}",
					Doctors = new List<AIAvailableSlotsForServiceDoctor>()
				};
			}
		}

		/// <summary>Lấy danh sách TẤT CẢ bác sĩ của một liệu trình</summary>
		public async Task<AIServiceDoctorsResponse> GetDoctorsForTreatmentPlanAsync(int treatmentPlanId)
		{
			try
			{
				_logger.LogInformation("GET_DOCTORS_FOR_TREATMENT_PLAN: treatmentPlanId={TreatmentPlanId}", treatmentPlanId);

				var response = new AIServiceDoctorsResponse { Doctors = new List<AIServiceDoctor>() };

				// ✅ STEP 1: Kiểm tra liệu trình tồn tại
				var treatmentPlan = await _treatmentPlanRepository.GetById(treatmentPlanId);
				if (treatmentPlan == null || treatmentPlan.DeleteStatus)
				{
					response.Message = "Liệu trình không tồn tại";
					response.Success = false;
					return response;
				}

				// ✅ STEP 2: Lấy service của liệu trình
				if (!treatmentPlan.ServiceId.HasValue)
				{
					response.Message = "Liệu trình này không được gán cho dịch vụ nào";
					response.Success = false;
					return response;
				}

				var service = await _serviceRepository.GetById(treatmentPlan.ServiceId.Value);
				if (service == null || service.DeleteStatus)
				{
					response.Message = "Dịch vụ của liệu trình không tồn tại";
					response.Success = false;
					return response;
				}

				response.ServiceName = service.ServiceName;
				response.ServiceDescription = service.Description;

				// ✅ STEP 3: Lấy TẤT CẢ bác sĩ có làm dịch vụ này
				// Cách 1: Lấy từ ClinicStaff (tất cả bác sĩ hoạt động)
				var allClinicStaffs = await _clinicStaffRepository.FindByPredicate(x =>
					!x.DeleteStatus);

				var staffIds = allClinicStaffs.Select(x => x.StaffId).Distinct().ToList();

				_logger.LogInformation("Found {StaffCount} staff members", staffIds.Count);

				// ✅ STEP 4: Lấy thông tin chi tiết bác sĩ + số lượng appointments
				var doctors = new List<AIServiceDoctor>();
				foreach (var staffId in staffIds)
				{
					var staff = await _staffRepository.GetById(staffId ?? 0);
					if (staff != null && staff.IsDoctor == true && !staff.DeleteStatus)
					{
						// Lấy số lượng lịch hẹn của bác sĩ cho dịch vụ này
						var appointmentCount = (await _appointmentRepository.FindByPredicate(x =>
							x.StaffId == staffId &&
							x.ServiceId == service.Id &&
							x.Status != (int)AppointmentStatus.Cancelled &&
							!x.DeleteStatus)).Count();

						// ✅ THÊM TẤT CẢ bác sĩ, bất kể có appointment hay không
						doctors.Add(new AIServiceDoctor
						{
							StaffId = staff.Id,
							Name = staff.FullName,
							Specialization = staff.Specialization,
							Experience = staff.ExperienceYears ?? 0,
							Degree = staff.Degree,
							Rating = appointmentCount > 0 ? CalculateDoctorRating(appointmentCount) : 0,
							AppointmentCount = appointmentCount
						});

						_logger.LogInformation("Doctor: {Name}, Appointments: {Count}", staff.FullName, appointmentCount);
					}
				}

				// ✅ Sort: bác sĩ có nhiều appointments trước
				response.Doctors = doctors
					.OrderByDescending(x => x.AppointmentCount)
					.ThenBy(x => x.Name)
					.ToList();

				response.Success = true;
				response.Message = $"Tìm thấy {doctors.Count} bác sĩ cho liệu trình {treatmentPlan.PlanName}";

				_logger.LogInformation("Total doctors found: {Count}", doctors.Count);

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_DOCTORS_FOR_TREATMENT_PLAN_ERROR: Exception occurred");
				return new AIServiceDoctorsResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}",
					Doctors = new List<AIServiceDoctor>()
				};
			}
		}
	}
}