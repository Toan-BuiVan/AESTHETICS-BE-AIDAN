using Aesthetics.Data.AestheticsInterfaces.EmailService;
using Aesthetics.Data.RepositoryInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aesthetics.Data.AestheticsServices.EmailService
{
	public class AppointmentReminderBackgroundService : BackgroundService
	{
		private readonly ILogger<AppointmentReminderBackgroundService> _logger;
		private readonly IServiceProvider _serviceProvider;

		public AppointmentReminderBackgroundService(
			ILogger<AppointmentReminderBackgroundService> logger,
			IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("AppointmentReminderBackgroundService started");

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await ProcessAppointmentReminders();
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "AppointmentReminderBackgroundService: Exception occurred");
				}

				// Chạy mỗi 1 giờ
				await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
			}
		}

		private async Task ProcessAppointmentReminders()
		{
			using var scope = _serviceProvider.CreateScope();
			var appointmentRepository = scope.ServiceProvider.GetRequiredService<IAppointmentRepositoty>(); // Giữ nguyên tên này vì đây là tên thật trong project
			var customerRepository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
			var staffRepository = scope.ServiceProvider.GetRequiredService<IStaffRepository>();
			var serviceRepository = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
			var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

			try
			{
				// Lấy các appointment cần gửi nhắc nhở
				var appointmentsNeedingReminder = await appointmentRepository.FindByPredicate(x =>
					!x.DeleteStatus &&
					x.StartTime.HasValue &&
					!x.IsReminderEmailSent &&
					x.StartTime.Value > DateTime.UtcNow && // Chưa qua giờ hẹn
					x.StartTime.Value <= DateTime.UtcNow.AddHours(x.ReminderHoursBefore) // Trong khoảng thời gian nhắc nhở
				);

				if (!appointmentsNeedingReminder.Any())
				{
					_logger.LogInformation("ProcessAppointmentReminders: No appointments need reminder");
					return;
				}

				_logger.LogInformation("ProcessAppointmentReminders: Found {Count} appointments need reminder",
					appointmentsNeedingReminder.Count());

				foreach (var appointment in appointmentsNeedingReminder)
				{
					try
					{
						await SendReminderForAppointment(appointment, customerRepository, staffRepository, serviceRepository, emailService, appointmentRepository);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "ProcessAppointmentReminders: Failed to process AppointmentId {AppointmentId}", appointment.Id);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ProcessAppointmentReminders: Exception occurred");
			}
		}

		private async Task SendReminderForAppointment(
			Entities.Entities.AppointmentEntity appointment,
			ICustomerRepository customerRepository,
			IStaffRepository staffRepository,
			IServiceRepository serviceRepository,
			IEmailService emailService,
			IAppointmentRepositoty appointmentRepository) // Thêm param này
		{
			try
			{
				// Lấy thông tin customer
				var customer = await customerRepository.GetById(appointment.CustomerId.Value);
				if (customer == null || string.IsNullOrEmpty(customer.Email))
				{
					_logger.LogWarning("SendReminderForAppointment: Customer not found or no email. AppointmentId {AppointmentId}", appointment.Id);
					return;
				}

				// Lấy thông tin staff
				var staff = await staffRepository.GetById(appointment.StaffId.Value);
				var staffName = staff?.FullName ?? "Nhân viên";

				// Lấy thông tin service
				var service = await serviceRepository.GetById(appointment.ServiceId.Value);
				var serviceName = service?.ServiceName ?? "Dịch vụ";

				// Gửi email nhắc nhở
				var emailSent = await emailService.SendAppointmentReminder(
					customer.Email,
					customer.FullName ?? "Khách hàng",
					serviceName,
					appointment.StartTime.Value,
					staffName
				);

				if (emailSent)
				{
					// Cập nhật trạng thái đã gửi email nhắc nhở
					appointment.IsReminderEmailSent = true;
					appointment.ReminderEmailSentDate = DateTime.UtcNow;
					await appointmentRepository.UpdateEntity(appointment);

					_logger.LogInformation("SendReminderForAppointment: Successfully sent reminder to {Email} for AppointmentId {AppointmentId}",
						customer.Email, appointment.Id);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SendReminderForAppointment: Exception for AppointmentId {AppointmentId}", appointment.Id);
			}
		}
	}
}

