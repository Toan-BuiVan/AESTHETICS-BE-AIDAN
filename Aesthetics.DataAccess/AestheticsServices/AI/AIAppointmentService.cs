using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.AI;
using Aesthetics.Data.RepositoryInterfaces;
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
		private readonly ICustomerTreatmentPlansRepository _customerTreatmentPlansRepository;
		private readonly ICustomerTreatmentSessionsRepository _customerTreatmentSessionsRepository;
		private readonly IAppointmentService _appointmentService;

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
			ICustomerTreatmentPlansRepository customerTreatmentPlansRepository,
			ICustomerTreatmentSessionsRepository customerTreatmentSessionsRepository,
			IAppointmentService appointmentService)
		{
			_logger = logger;
			_appointmentRepository = appointmentRepository;
			_staffRepository = staffRepository;
			_serviceRepository = serviceRepository;
			_appointmentTimeLockRepository = appointmentTimeLockRepository;
			_clinicStaffRepository = clinicStaffRepository;
			_treatmentPlanRepository = treatmentPlanRepository;
			_customerTreatmentPlansRepository = customerTreatmentPlansRepository;
			_customerTreatmentSessionsRepository = customerTreatmentSessionsRepository;
			_appointmentService = appointmentService;
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

				// Lấy tất cả bác sĩ từ ClinicStaff (không filter theo service vì ClinicStaff không có ServiceId)
				// Thay vào đó, lấy tất cả bác sĩ và lọc những bác sĩ có lịch hẹn cho dịch vụ này
				var allClinicStaffs = await _clinicStaffRepository.FindByPredicate(x =>
					!x.DeleteStatus);

				var staffIds = allClinicStaffs.Select(x => x.StaffId).Distinct().ToList();

				// Lấy thông tin bác sĩ
				var doctors = new List<AIServiceDoctor>();
				foreach (var staffId in staffIds)
				{
					var staff = await _staffRepository.GetById(staffId ?? 0);
					if (staff != null && staff.IsDoctor == true && !staff.DeleteStatus)
					{
						// Lấy số lượng lịch hẹn của bác sĩ cho dịch vụ này
						var appointmentCount = (await _appointmentRepository.FindByPredicate(x =>
							x.StaffId == staffId &&
							x.ServiceId == serviceId &&
							x.Status != (int)AppointmentStatus.Cancelled &&
							!x.DeleteStatus)).Count();

						// Chỉ thêm bác sĩ nếu có lịch hẹn cho dịch vụ này
						if (appointmentCount > 0)
						{
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
		public async Task<AIBookAppointmentResponse> BookAppointmentAsync(int customerId, int staffId, int serviceId, DateTime appointmentDate, string appointmentTime, int? treatmentPlanId = null)
		{
			try
			{
				_logger.LogInformation("BOOK_APPOINTMENT: customerId={CustomerId}, staffId={StaffId}, serviceId={ServiceId}, date={Date}, time={Time}", 
					customerId, staffId, serviceId, appointmentDate.Date, appointmentTime);

				var response = new AIBookAppointmentResponse();

				// Parse thời gian
				if (!TimeSpan.TryParse(appointmentTime, out var timeSpan))
				{
					response.Success = false;
					response.Message = "Định dạng thời gian không hợp lệ (sử dụng HH:mm)";
					return response;
				}

				var startTime = appointmentDate.Date.Add(timeSpan);

				// Tạo appointment - không truyền ServiceId vào CreateAppointment
				// vì nó sẽ được lấy từ TreatmentPlan hoặc Service
				var createAppointment = new CreateAppointment
				{
					CustomerId = customerId,
					StaffId = staffId,
					StartTime = startTime,
					CustomerTreatmentPlanId = treatmentPlanId
				};

				var isBooked = await _appointmentService.create(createAppointment);
				if (!isBooked)
				{
					response.Success = false;
					response.Message = "Không thể đặt lịch. Vui lòng kiểm tra thông tin";
					return response;
				}

				// Lấy thông tin lịch hẹn vừa tạo
				var appointments = await _appointmentRepository.FindByPredicate(x =>
					x.CustomerId == customerId &&
					x.StaffId == staffId &&
					x.StartTime == startTime &&
					!x.DeleteStatus);

				var appointment = appointments.FirstOrDefault();
				if (appointment != null)
				{
					response.Success = true;
					response.AppointmentId = appointment.Id;
					response.Message = $"Đặt lịch thành công vào lúc {startTime:HH:mm} ngày {startTime:dd/MM/yyyy}";
				}

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "BOOK_APPOINTMENT_ERROR: Exception occurred");
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

				// Tìm lịch hẹn cần hủy
				var appointments = await _appointmentRepository.FindByPredicate(x =>
					x.CustomerId == customerId &&
					x.StaffId == staffId &&
					x.Status != (int)AppointmentStatus.Cancelled &&
					!x.DeleteStatus);

				// Lọc theo ngày nếu có
				if (appointmentDate.HasValue)
				{
					appointments = appointments
						.Where(x => x.StartTime!.Value.Date == appointmentDate.Value.Date)
						.ToList();
				}

				// Lọc theo dịch vụ nếu có
				if (serviceId.HasValue)
				{
					appointments = appointments
						.Where(x => x.ServiceId == serviceId)
						.ToList();
				}

				if (!appointments.Any())
				{
					response.Success = false;
					response.Message = "Không tìm thấy lịch hẹn để hủy";
					return response;
				}

				// Hủy tất cả lịch hẹn tìm được
				int cancelledCount = 0;
				foreach (var appointment in appointments)
				{
					appointment.Status = (int)AppointmentStatus.Cancelled;
					appointment.DeleteStatus = true;
					var updated = await _appointmentRepository.UpdateEntity(appointment);
					if (updated)
						cancelledCount++;
				}

				response.Success = true;
				response.CancelledCount = cancelledCount;
				response.Message = $"Đã hủy {cancelledCount} lịch hẹn";

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