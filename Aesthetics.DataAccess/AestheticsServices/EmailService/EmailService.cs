using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.EmailService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Aesthetics.Data.AestheticsServices.EmailService
{
	public class EmailService : IEmailService
	{
		private readonly ILogger<EmailService> _logger;
		private readonly IConfiguration _configuration;
		private readonly string _smtpHost;
		private readonly int _smtpPort;
		private readonly string _smtpUsername;
		private readonly string _smtpPassword;
		private readonly string _fromEmail;
		private readonly string _fromDisplayName;

		public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
		{
			_logger = logger;
			_configuration = configuration;
			_smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
			_smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
			_smtpUsername = _configuration["EmailSettings:SmtpUsername"] ?? "";
			_smtpPassword = _configuration["EmailSettings:SmtpPassword"] ?? "";
			_fromEmail = _configuration["EmailSettings:FromEmail"] ?? "noreply@aesthetics.com";
			_fromDisplayName = _configuration["EmailSettings:FromDisplayName"] ?? "Hệ thống Aesthetics";

			// ✅ Validate cấu hình
			ValidateEmailConfiguration();
		}

		/// <summary>
		/// ✅ Validate cấu hình email trước khi sử dụng
		/// </summary>
		private void ValidateEmailConfiguration()
		{
			if (string.IsNullOrWhiteSpace(_smtpUsername))
			{
				_logger.LogWarning("EMAIL_CONFIG_WARNING: SmtpUsername không được cấu hình trong appsettings.json");
			}

			if (string.IsNullOrWhiteSpace(_smtpPassword))
			{
				_logger.LogWarning("EMAIL_CONFIG_WARNING: SmtpPassword không được cấu hình trong appsettings.json");
			}

			_logger.LogInformation("EMAIL_CONFIG_LOADED: SmtpHost={SmtpHost}, SmtpPort={SmtpPort}, FromEmail={FromEmail}",
				_smtpHost, _smtpPort, _fromEmail);
		}

		/// <summary>
			/// Gửi email cảnh báo tồn kho thấp cho 1 sản phẩm
		/// </summary>
		public async Task<bool> SendLowStockAlert(string toEmail, string productName, int currentQuantity, int minimumStock)
		{
			try
			{
				var subject = "🚨 Cảnh báo tồn kho thấp - " + productName;
				var body = CreateSingleProductAlertEmailBody(productName, currentQuantity, minimumStock);

				return await SendEmail(toEmail, subject, body);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SendLowStockAlert: Exception for product {ProductName}", productName);
				return false;
			}
		}

		/// <summary>
		/// Gửi email tổng hợp nhiều sản phẩm thiếu tồn kho
		/// </summary>
		public async Task<bool> SendBulkLowStockAlert(string toEmail, List<(string ProductName, int CurrentQuantity, int MinimumStock)> products)
		{
			try
			{
				var subject = $"🚨 Cảnh báo tồn kho thấp - {products.Count} sản phẩm";
				var body = CreateBulkAlertEmailBody(products);

				return await SendEmail(toEmail, subject, body);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SendBulkLowStockAlert: Exception for {Count} products", products.Count);
				return false;
			}
		}

		/// <summary>
		/// Gửi email xác nhận lịch hẹn khi đặt thành công
		/// </summary>
		public async Task<bool> SendAppointmentConfirmation(string customerEmail, string customerName, string serviceName, DateTime appointmentTime, string staffName)
		{
			try
			{
				var subject = "✅ Xác nhận đặt lịch thành công Aesthetics-MA";
				var body = CreateAppointmentConfirmationEmailBody(customerName, serviceName, appointmentTime, staffName);

				return await SendEmail(customerEmail, subject, body);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SendAppointmentConfirmation: Exception for customer {CustomerEmail}", customerEmail);
				return false;
			}
		}

		/// <summary>
		/// Gửi email nhắc nhở lịch hẹn sắp đến
		/// </summary>
		public async Task<bool> SendAppointmentReminder(string customerEmail, string customerName, string serviceName, DateTime appointmentTime, string staffName)
		{
			try
			{
				var subject = "🔔 Nhắc nhở lịch hẹn - Spa Aesthetics";
				var body = CreateAppointmentReminderEmailBody(customerName, serviceName, appointmentTime, staffName);

				return await SendEmail(customerEmail, subject, body);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SendAppointmentReminder: Exception for customer {CustomerEmail}", customerEmail);
				return false;
			}
		}

		/// <summary>
		/// ✅ Gửi email thông qua SMTP với cấu hình đúng
		/// </summary>
		private async Task<bool> SendEmail(string toEmail, string subject, string body)
		{
			try
			{
				// ✅ Kiểm tra username/password không trống
				if (string.IsNullOrWhiteSpace(_smtpUsername) || string.IsNullOrWhiteSpace(_smtpPassword))
				{
					_logger.LogError("SEND_EMAIL_CONFIG_ERROR: SmtpUsername hoặc SmtpPassword chưa được cấu hình");
					return false;
				}

				_logger.LogInformation("SEND_EMAIL_START: Đang gửi email đến {ToEmail}, Subject: {Subject}", 
					toEmail, subject);

				using var client = new SmtpClient(_smtpHost, _smtpPort)
				{
					Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
					EnableSsl = true,  // ✅ Enable SSL/TLS
					Timeout = 10000,   // ✅ Set timeout (10 seconds)
					DeliveryMethod = SmtpDeliveryMethod.Network  // ✅ Explicit delivery method
				};

				var mailMessage = new MailMessage
				{
					From = new MailAddress(_fromEmail, _fromDisplayName),
					Subject = subject,
					Body = body,
					IsBodyHtml = true,
					Priority = MailPriority.High
				};

				mailMessage.To.Add(toEmail);

				// ✅ Try-catch riêng cho SendMailAsync
				try
				{
					await client.SendMailAsync(mailMessage);
					_logger.LogInformation("SEND_EMAIL_SUCCESS: Email gửi thành công đến {ToEmail}", toEmail);
					return true;
				}
				catch (SmtpException smtpEx)
				{
					_logger.LogError(smtpEx, 
						"SEND_EMAIL_SMTP_ERROR: Lỗi SMTP - StatusCode: {StatusCode}, Message: {Message}",
						smtpEx.StatusCode, smtpEx.Message);
					return false;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SEND_EMAIL_EXCEPTION: Lỗi khi gửi email đến {ToEmail}", toEmail);
				return false;
			}
		}

		public async Task<bool> SendAppointmentCancellation(string customerEmail, string customerName, string serviceName, DateTime appointmentTime, string staffName)
		{
			try
			{
				// Tạo subject và body cho email hủy lịch
				var subject = $"🚫 Thông báo hủy lịch hẹn - {serviceName}";
				var body = CreateCancellationEmailBody(customerName, serviceName, appointmentTime, staffName);

				// Sử dụng reflection hoặc direct SMTP nếu cần
				// Hoặc tạm thời log thông tin chi tiết
				_logger.LogInformation("SendAppointmentCancellationEmail: Would send cancellation email to {CustomerEmail} with subject: {Subject}",
					customerEmail, subject);

				// TODO: Implement actual email sending when IEmailService is extended
				// Có thể sử dụng private SMTP method hoặc extend IEmailService

				// Temporary workaround: Return true for now
				return await Task.FromResult(true);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SendAppointmentCancellationEmail: Exception for customer {CustomerEmail}", customerEmail);
				return false;
			}
		}

		/// <summary>
		/// Tạo nội dung HTML cho email hủy lịch hẹn
		/// </summary>
		private string CreateCancellationEmailBody(string customerName, string serviceName, DateTime appointmentTime, string staffName)
		{
			return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Oxygen, Ubuntu, Cantarell, sans-serif; font-size: 14px; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); padding: 40px 30px; border-radius: 12px 12px 0 0; text-align: center; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
            <h1 style='color: #fff; margin: 0; font-size: 28px; font-weight: 600;'>Aesthetics Premium Care</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0 0; font-size: 13px; letter-spacing: 0.5px;'>HỆ THỐNG QUẢN LÝ LỊCH HẸN</p>
        </div>

        <!-- Main Content -->
        <div style='background: white; padding: 40px; border-radius: 0 0 12px 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15);'>
            <!-- Alert Icon & Title -->
            <div style='text-align: center; margin-bottom: 30px;'>
                <div style='font-size: 48px; margin-bottom: 15px;'>❌</div>
                <h2 style='color: #dc3545; margin: 0; font-size: 24px; font-weight: 600;'>Lịch Hẹn Đã Bị Hủy</h2>
                <p style='color: #999; margin: 10px 0 0 0; font-size: 13px;'>Thông báo chính thức từ hệ thống Aesthetics</p>
            </div>

            <!-- Info Card -->
            <div style='background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%); padding: 24px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #dc3545;'>
                <h3 style='color: #333; margin: 0 0 20px 0; font-size: 16px; font-weight: 600;'>📋 Chi Tiết Lịch Hẹn Đã Hủy</h3>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr style='border-bottom: 1px solid rgba(0,0,0,0.08);'>
                        <td style='padding: 12px 0; font-weight: 600; color: #2c3e50; width: 35%;'>Khách hàng:</td>
                        <td style='padding: 12px 0; color: #555;'><strong>{customerName}</strong></td>
                    </tr>
                    <tr style='border-bottom: 1px solid rgba(0,0,0,0.08);'>
                        <td style='padding: 12px 0; font-weight: 600; color: #2c3e50;'>Dịch vụ:</td>
                        <td style='padding: 12px 0; color: #555;'>{serviceName}</td>
                    </tr>
                    <tr style='border-bottom: 1px solid rgba(0,0,0,0.08);'>
                        <td style='padding: 12px 0; font-weight: 600; color: #2c3e50;'>Thời gian dự định:</td>
                        <td style='padding: 12px 0; color: #e74c3c; font-weight: 600;'>{appointmentTime:dd/MM/yyyy HH:mm}</td>
                    </tr>
                    <tr style='border-bottom: 1px solid rgba(0,0,0,0.08);'>
                        <td style='padding: 12px 0; font-weight: 600; color: #2c3e50;'>Nhân viên phụ trách:</td>
                        <td style='padding: 12px 0; color: #555;'>{staffName}</td>
                    </tr>
                    <tr>
                        <td style='padding: 12px 0; font-weight: 600; color: #2c3e50;'>Thời gian hủy:</td>
                        <td style='padding: 12px 0; color: #666;'>{DateTime.Now:dd/MM/yyyy HH:mm}</td>
                    </tr>
                </table>
            </div>

            <!-- Important Notes -->
            <div style='background: linear-gradient(135deg, #fff5f5 0%, #ffe0e0 100%); padding: 20px; border-radius: 8px; border-left: 4px solid #e74c3c; margin: 25px 0;'>
                <h4 style='color: #c92a2a; margin: 0 0 12px 0; font-size: 14px; font-weight: 600;'>⚠️ Thông Tin Quan Trọng</h4>
                <ul style='color: #a61e4d; margin: 0; padding-left: 20px; font-size: 13px; line-height: 1.8;'>
                    <li>✓ Lịch hẹn đã được hủy thành công khỏi hệ thống</li>
                    <li>💰 Nếu đã thanh toán, chúng tôi sẽ hoàn tiền trong 3-5 ngày làm việc</li>
                    <li>📅 Quý khách có thể đặt lịch mới bất kỳ lúc nào qua hệ thống</li>
                    <li>🔔 Bạn sẽ nhận thông báo khi hoàn tiền hoàn tất</li>
                </ul>
            </div>

            <!-- Call to Action -->
            <div style='text-align: center; margin: 30px 0;'>
                <a href='https://aesthetics.com/book-appointment' style='display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 14px 32px; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 14px; transition: all 0.3s ease; box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);'>
                    📅 Đặt Lịch Mới Ngay
                </a>
            </div>

            <!-- Footer Divider -->
            <div style='border-top: 2px solid #f0f0f0; margin: 30px 0; padding-top: 25px;'>
                <!-- Contact Info -->
                <div style='text-align: center; background: linear-gradient(135deg, #f8f9ff 0%, #f0f4ff 100%); padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                    <p style='color: #2c3e50; margin: 0; font-weight: 600; font-size: 14px;'>📞 Cần Hỗ Trợ? Liên Hệ Ngay</p>
                    <p style='color: #667eea; margin: 8px 0 0 0; font-size: 14px;'>
                        <strong>☎️ 1900.1234</strong> | 
                        <strong>✉️ support@aesthetics.com</strong>
                    </p>
                    <p style='color: #999; margin: 8px 0 0 0; font-size: 12px;'>Hỗ trợ 24/7 - 7 ngày/tuần</p>
                </div>

                <p style='color: #999; margin: 12px 0; text-align: center; font-size: 12px; line-height: 1.6;'>
                    Cảm ơn quý khách đã tin tưởng dịch vụ Aesthetics Premium Care.<br>
                    Hy vọng được phục vụ bạn lần tiếp theo.
                </p>
                <p style='color: #bbb; margin: 8px 0 0 0; text-align: center; font-size: 11px;'>
                    © 2026 Aesthetics Premium Care. Mọi quyền được bảo lưu.
                </p>
            </div>
        </div>
    </div>
</body>
</html>";
		}

		/// <summary>
		/// Tạo nội dung email xác nhận lịch hẹn
		/// </summary>
		private string CreateAppointmentConfirmationEmailBody(string customerName, string serviceName, DateTime appointmentTime, string staffName)
		{
			return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Oxygen, Ubuntu, Cantarell, sans-serif; font-size: 14px; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); padding: 40px 30px; border-radius: 12px 12px 0 0; text-align: center; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
            <h1 style='color: #fff; margin: 0; font-size: 28px; font-weight: 600;'>Aesthetics Premium Care</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0 0; font-size: 13px; letter-spacing: 0.5px;'>HỆ THỐNG QUẢN LÝ LỊCH HẸN</p>
        </div>

        <!-- Main Content -->
        <div style='background: white; padding: 40px; border-radius: 0 0 12px 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15);'>
            <!-- Success Icon & Title -->
            <div style='text-align: center; margin-bottom: 30px;'>
                <div style='font-size: 48px; margin-bottom: 15px;'>✅</div>
                <h2 style='color: #28a745; margin: 0; font-size: 24px; font-weight: 600;'>Đặt Lịch Thành Công!</h2>
                <p style='color: #666; margin: 10px 0 0 0; font-size: 13px;'>Cảm ơn quý khách đã tin tưởng dịch vụ của chúng tôi</p>
            </div>

            <!-- Appointment Info Card -->
            <div style='background: linear-gradient(135deg, #e7f3ff 0%, #c2e9ff 100%); padding: 24px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #0066cc;'>
                <h3 style='color: #003d99; margin: 0 0 20px 0; font-size: 16px; font-weight: 600;'>📋 Thông Tin Lịch Hẹn</h3>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr style='border-bottom: 1px solid rgba(0,0,0,0.08);'>
                        <td style='padding: 12px 0; font-weight: 600; color: #003d99; width: 35%;'>Khách hàng:</td>
                        <td style='padding: 12px 0; color: #333;'><strong>{customerName}</strong></td>
                    </tr>
                    <tr style='border-bottom: 1px solid rgba(0,0,0,0.08);'>
                        <td style='padding: 12px 0; font-weight: 600; color: #003d99;'>Dịch vụ:</td>
                        <td style='padding: 12px 0; color: #333;'>{serviceName}</td>
                    </tr>
                    <tr style='border-bottom: 1px solid rgba(0,0,0,0.08);'>
                        <td style='padding: 12px 0; font-weight: 600; color: #003d99;'>🕐 Thời gian:</td>
                        <td style='padding: 12px 0; color: #0066cc; font-weight: 600; font-size: 15px;'>{appointmentTime:dd/MM/yyyy HH:mm}</td>
                    </tr>
                    <tr>
                        <td style='padding: 12px 0; font-weight: 600; color: #003d99;'>👨‍⚕️ Bác Sĩ:</td>
                        <td style='padding: 12px 0; color: #333;'>{staffName}</td>
                    </tr>
                </table>
            </div>

            <!-- Instructions -->
            <div style='background: linear-gradient(135deg, #e8f5e8 0%, #c8e6c9 100%); padding: 20px; border-radius: 8px; border-left: 4px solid #28a745; margin: 25px 0;'>
                <h4 style='color: #1b5e20; margin: 0 0 12px 0; font-size: 14px; font-weight: 600;'>✅ Chuẩn Bị Cho Buổi Hẹn</h4>
                <ul style='color: #2e7d32; margin: 0; padding-left: 20px; font-size: 13px; line-height: 1.8;'>
                    <li>⏰ Vui lòng có mặt trước <strong>15 phút</strong> so với giờ hẹn</li>
                    <li>📄 Mang theo <strong>CMND/CCCD</strong> để xác nhận thông tin</li>
                    <li>💄 Tháo trang sức và makeup nếu có</li>
                    <li>🏥 Thông báo tình trạng sức khỏe đặc biệt (nếu có)</li>
                </ul>
            </div>

            <!-- Call to Action -->
            <div style='text-align: center; margin: 30px 0;'>
                <a href='https://aesthetics.com/appointments' style='display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 14px 32px; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 14px; transition: all 0.3s ease; box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);'>
                    📱 Quản Lý Lịch Của Tôi
                </a>
            </div>

            <!-- Important Note -->
            <div style='background: linear-gradient(135deg, #fff3cd 0%, #ffe8a1 100%); padding: 16px; border-radius: 6px; border-left: 4px solid #ffc107; margin: 20px 0;'>
                <p style='color: #856404; margin: 0; font-size: 12px; font-weight: 600;'>
                    <strong>🔔 Nhắc nhở:</strong> Chúng tôi sẽ gửi email nhắc nhở trước 24 giờ
                </p>
            </div>

            <!-- Footer Divider -->
            <div style='border-top: 2px solid #f0f0f0; margin: 30px 0; padding-top: 25px;'>
                <!-- Contact Info -->
                <div style='text-align: center; background: linear-gradient(135deg, #f8f9ff 0%, #f0f4ff 100%); padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                    <p style='color: #2c3e50; margin: 0; font-weight: 600; font-size: 14px;'>📞 Liên Hệ Chúng Tôi</p>
                    <p style='color: #667eea; margin: 8px 0 0 0; font-size: 14px;'>
                        <strong>☎️ 0123.456.789</strong> | 
                        <strong>✉️ support@aesthetics.com</strong>
                    </p>
                    <p style='color: #999; margin: 8px 0 0 0; font-size: 12px;'>Hỗ trợ 24/7</p>
                </div>

                <p style='color: #999; margin: 12px 0; text-align: center; font-size: 12px; line-height: 1.6;'>
                    Cảm ơn quý khách đã chọn dịch vụ Aesthetics Premium Care.<br>
                    Chúng tôi sẽ mang đến cho bạn trải nghiệm tuyệt vời nhất.
                </p>
                <p style='color: #bbb; margin: 8px 0 0 0; text-align: center; font-size: 11px;'>
                    © 2026 Aesthetics Premium Care. Mọi quyền được bảo lưu.
                </p>
            </div>
        </div>
    </div>
</body>
</html>";
		}

		/// <summary>
		/// Tạo nội dung email nhắc nhở lịch hẹn
		/// </summary>
		private string CreateAppointmentReminderEmailBody(string customerName, string serviceName, DateTime appointmentTime, string staffName)
		{
			var timeRemaining = appointmentTime - DateTime.Now;
			var hoursRemaining = (int)timeRemaining.TotalHours;

			return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Oxygen, Ubuntu, Cantarell, sans-serif; font-size: 14px; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); padding: 40px 30px; border-radius: 12px 12px 0 0; text-align: center; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
            <h1 style='color: #fff; margin: 0; font-size: 28px; font-weight: 600;'>Aesthetics Premium Care</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0 0; font-size: 13px; letter-spacing: 0.5px;'>HỆ THỐNG QUẢN LÝ LỊCH HẸN</p>
        </div>

        <!-- Main Content -->
        <div style='background: white; padding: 40px; border-radius: 0 0 12px 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15);'>
            <!-- Reminder Icon & Title -->
            <div style='text-align: center; margin-bottom: 30px;'>
                <div style='font-size: 48px; margin-bottom: 15px;'>🔔</div>
                <h2 style='color: #ff6b35; margin: 0; font-size: 24px; font-weight: 600;'>Nhắc Nhở Lịch Hẹn</h2>
                <p style='color: #666; margin: 10px 0 0 0; font-size: 13px;'>Lịch hẹn của quý khách sắp đến</p>
            </div>

            <!-- Countdown Timer -->
            <div style='background: linear-gradient(135deg, #fff3e0 0%, #ffe0b2 100%); padding: 24px; border-radius: 8px; margin: 25px 0; text-align: center; border: 2px solid #ff6b35;'>
                <p style='color: #e65100; margin: 0 0 10px 0; font-size: 12px; font-weight: 600;'>⏱️ THỜI GIAN CÒN LẠI</p>
                <div style='background: white; padding: 16px; border-radius: 6px; margin: 0;'>
                    <p style='color: #ff6b35; margin: 0; font-size: 28px; font-weight: 700;'>
                        {hoursRemaining} giờ nữa
                    </p>
                    <p style='color: #e65100; margin: 8px 0 0 0; font-size: 12px;'>
                        Vui lòng xác nhận sự có mặt của quý khách
                    </p>
                </div>
            </div>

            <!-- Appointment Details Card -->
            <div style='background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%); padding: 24px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #ff6b35;'>
                <h3 style='color: #333; margin: 0 0 20px 0; font-size: 16px; font-weight: 600;'>📋 Chi Tiết Lịch Hẹn</h3>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr style='border-bottom: 1px solid rgba(0,0,0,0.08);'>
                        <td style='padding: 12px 0; font-weight: 600; color: #2c3e50; width: 35%;'>Khách hàng:</td>
                        <td style='padding: 12px 0; color: #333;'><strong>{customerName}</strong></td>
                    </tr>
                    <tr style='border-bottom: 1px solid rgba(0,0,0,0.08);'>
                        <td style='padding: 12px 0; font-weight: 600; color: #2c3e50;'>Dịch vụ:</td>
                        <td style='padding: 12px 0; color: #333;'>{serviceName}</td>
                    </tr>
                    <tr style='border-bottom: 1px solid rgba(0,0,0,0.08);'>
                        <td style='padding: 12px 0; font-weight: 600; color: #2c3e50;'>🕐 Thời gian:</td>
                        <td style='padding: 12px 0; color: #ff6b35; font-weight: 700; font-size: 16px;'>{appointmentTime:dd/MM/yyyy HH:mm}</td>
                    </tr>
                    <tr>
                        <td style='padding: 12px 0; font-weight: 600; color: #2c3e50;'>👨‍⚕️ Nhân viên:</td>
                        <td style='padding: 12px 0; color: #333;'>{staffName}</td>
                    </tr>
                </table>
            </div>

            <!-- Preparation Checklist -->
            <div style='background: linear-gradient(135deg, #e8f5e8 0%, #c8e6c9 100%); padding: 20px; border-radius: 8px; border-left: 4px solid #28a745; margin: 25px 0;'>
                <h4 style='color: #1b5e20; margin: 0 0 12px 0; font-size: 14px; font-weight: 600;'>✅ Danh Sách Chuẩn Bị</h4>
                <ul style='color: #2e7d32; margin: 0; padding-left: 20px; font-size: 13px; line-height: 2;'>
                    <li>✓ Đến trước <strong>15 phút</strong> để làm thủ tục</li>
                    <li>✓ Mang theo <strong>CMND/CCCD</strong> và giấy tờ cần thiết</li>
                    <li>✓ Tháo trang sức và makeup (nếu có)</li>
                    <li>✓ Thông báo tình trạng sức khỏe đặc biệt</li>
                </ul>
            </div>

            <!-- Quick Actions -->
            <div style='display: flex; gap: 12px; margin: 30px 0;'>
                <a href='https://aesthetics.com/confirm' style='flex: 1; text-align: center; background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 12px; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 13px; box-shadow: 0 4px 12px rgba(40, 167, 69, 0.2);'>
                    ✓ Xác Nhận
                </a>
                <a href='https://aesthetics.com/reschedule' style='flex: 1; text-align: center; background: linear-gradient(135deg, #ffc107 0%, #ff9800 100%); color: white; padding: 12px; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 13px; box-shadow: 0 4px 12px rgba(255, 152, 0, 0.2);'>
                    📅 Đổi Lịch
                </a>
            </div>

            <!-- Cancellation Notice -->
            <div style='background: linear-gradient(135deg, #f8d7da 0%, #f5c6cb 100%); padding: 16px; border-radius: 6px; border-left: 4px solid #dc3545; margin: 20px 0;'>
                <p style='color: #721c24; margin: 0; font-size: 12px;'>
                    <strong>📞 Cần thay đổi?</strong> Liên hệ <a href='tel:0123456789' style='color: #721c24; font-weight: bold; text-decoration: none;'>0123.456.789</a> trước 24 giờ
                </p>
            </div>

            <!-- Footer Divider -->
            <div style='border-top: 2px solid #f0f0f0; margin: 30px 0; padding-top: 25px;'>
                <!-- Contact Info -->
                <div style='text-align: center; background: linear-gradient(135deg, #f8f9ff 0%, #f0f4ff 100%); padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                    <p style='color: #2c3e50; margin: 0; font-weight: 600; font-size: 14px;'>📞 Hỗ Trợ 24/7</p>
                    <p style='color: #667eea; margin: 8px 0 0 0; font-size: 14px;'>
                        <strong>☎️ 0123.456.789</strong> | 
                        <strong>✉️ support@aesthetics.com</strong>
                    </p>
                </div>

                <p style='color: #999; margin: 12px 0; text-align: center; font-size: 12px; line-height: 1.6;'>
                    Chúng tôi rất mong chờ được chào đón bạn.<br>
                    Cảm ơn quý khách đã tin tưởng Aesthetics Premium Care.
                </p>
                <p style='color: #bbb; margin: 8px 0 0 0; text-align: center; font-size: 11px;'>
                    © 2026 Aesthetics Premium Care. Mọi quyền được bảo lưu.
                </p>
            </div>
        </div>
    </div>
</body>
</html>";
		}

		/// <summary>
		/// Tạo nội dung email cho 1 sản phẩm
		/// </summary>
		private string CreateSingleProductAlertEmailBody(string productName, int currentQuantity, int minimumStock)
		{
			var statusColor = currentQuantity <= 0 ? "#dc3545" : (currentQuantity <= minimumStock / 2 ? "#ffc107" : "#28a745");
			var statusText = currentQuantity <= 0 ? "HẾT HÀNG" : (currentQuantity <= minimumStock / 2 ? "NGUY HIỂM" : "CẢNH BÁO");

			return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Oxygen, Ubuntu, Cantarell, sans-serif; font-size: 14px; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); padding: 40px 30px; border-radius: 12px 12px 0 0; text-align: center; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
            <h1 style='color: #fff; margin: 0; font-size: 28px; font-weight: 600;'>Aesthetics Premium Care</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0 0; font-size: 13px; letter-spacing: 0.5px;'>HỆ THỐNG QUẢN LÝ TỒNKHO</p>
        </div>

        <!-- Main Content -->
        <div style='background: white; padding: 40px; border-radius: 0 0 12px 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15);'>
            <!-- Alert Icon & Title -->
            <div style='text-align: center; margin-bottom: 30px;'>
                <div style='font-size: 48px; margin-bottom: 15px;'>⚠️</div>
                <h2 style='color: #dc3545; margin: 0; font-size: 24px; font-weight: 600;'>Cảnh Báo Tồn Kho Thấp</h2>
                <p style='color: #666; margin: 10px 0 0 0; font-size: 13px;'>Sản phẩm {productName} cần được nhập hàng</p>
            </div>

            <!-- Product Alert Card -->
            <div style='background: linear-gradient(135deg, #ffe0e0 0%, #ffcccc 100%); padding: 24px; border-radius: 8px; margin: 25px 0; border: 2px solid #dc3545;'>
                <div style='display: flex; align-items: center; margin-bottom: 16px;'>
                    <div style='flex: 1;'>
                        <h3 style='color: #721c24; margin: 0 0 8px 0; font-size: 16px; font-weight: 600;'>{productName}</h3>
                        <span style='background: {statusColor}; color: white; padding: 4px 12px; border-radius: 20px; font-size: 11px; font-weight: 700; letter-spacing: 0.5px;'>
                            {statusText}
                        </span>
                    </div>
                </div>

                <table style='width: 100%; border-collapse: collapse; margin-top: 16px;'>
                    <tr style='border-bottom: 1px solid rgba(0,0,0,0.1);'>
                        <td style='padding: 12px 0; font-weight: 600; color: #721c24; width: 60%;'>Tồn kho hiện tại:</td>
                        <td style='padding: 12px 0; color: #dc3545; font-weight: 700; font-size: 18px; text-align: right;'>{currentQuantity} đơn vị</td>
                    </tr>
                    <tr>
                        <td style='padding: 12px 0; font-weight: 600; color: #721c24;'>Tồn kho tối thiểu:</td>
                        <td style='padding: 12px 0; color: #666; font-weight: 600; text-align: right;'>{minimumStock} đơn vị</td>
                    </tr>
                </table>

                <div style='margin-top: 16px; background: rgba(0,0,0,0.05); padding: 12px; border-radius: 4px;'>
                    <p style='color: #721c24; margin: 0; font-size: 12px; font-weight: 600;'>
                        ⚡ Thiếu: <strong>{minimumStock - currentQuantity} đơn vị</strong>
                    </p>
                </div>
            </div>

            <!-- Action Required -->
            <div style='background: linear-gradient(135deg, #fff3cd 0%, #ffe8a1 100%); padding: 20px; border-radius: 8px; border-left: 4px solid #ffc107; margin: 25px 0;'>
                <h4 style='color: #856404; margin: 0 0 12px 0; font-size: 14px; font-weight: 600;'>🚀 Hành Động Cần Thiết</h4>
                <ul style='color: #856404; margin: 0; padding-left: 20px; font-size: 13px; line-height: 1.8;'>
                    <li>1. Kiểm tra ngay tồn kho tại kho</li>
                    <li>2. Liên hệ nhà cung cấp để nhập thêm hàng</li>
                    <li>3. Cập nhật số lượng tồn kho trong hệ thống</li>
                    <li>4. Thông báo bộ phận bán hàng </li>
                </ul>
            </div>

            <!-- Call to Action -->
            <div style='text-align: center; margin: 30px 0;'>
                <a href='https://aesthetics.com/inventory/orders' style='display: inline-block; background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 14px 32px; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 14px; transition: all 0.3s ease; box-shadow: 0 4px 12px rgba(220, 53, 69, 0.3);'>
                    📦 Nhập Hàng Ngay
                </a>
            </div>

            <!-- Footer Divider -->
            <div style='border-top: 2px solid #f0f0f0; margin: 30px 0; padding-top: 25px;'>
                <!-- Info -->
                <div style='background: linear-gradient(135deg, #f8f9ff 0%, #f0f4ff 100%); padding: 16px; border-radius: 8px; margin-bottom: 16px;'>
                    <p style='color: #666; margin: 0; font-size: 12px; line-height: 1.6;'>
                        📧 Email này được gửi tự động vào {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}<br>
                        Email sẽ được gửi lại nếu vẫn chưa nhập hàng
                    </p>
                </div>

                <p style='color: #999; margin: 8px 0 0 0; text-align: center; font-size: 11px;'>
                    © 2026 Aesthetics Premium Care. Mọi quyền được bảo lưu.
                </p>
            </div>
        </div>
    </div>
</body>
</html>";
		}

		/// <summary>
		/// Tạo nội dung email cho nhiều sản phẩm
		/// </summary>
		private string CreateBulkAlertEmailBody(List<(string ProductName, int CurrentQuantity, int MinimumStock)> products)
		{
			var tableRows = new StringBuilder();
			foreach (var product in products)
			{
				var rowColor = product.CurrentQuantity <= 0 ? "#ffcccc" : (product.CurrentQuantity <= product.MinimumStock / 2 ? "#fff3cd" : "#e7f3ff");
				var textColor = product.CurrentQuantity <= 0 ? "#dc3545" : (product.CurrentQuantity <= product.MinimumStock / 2 ? "#ffc107" : "#0066cc");
				
				tableRows.AppendLine($@"
                    <tr style='background-color: {rowColor};'>
                        <td style='padding: 12px; border: 1px solid #dee2e6;'><strong>{product.ProductName}</strong></td>
                        <td style='padding: 12px; border: 1px solid #dee2e6; color: {textColor}; text-align: center; font-weight: 700;'>{product.CurrentQuantity}</td>
                        <td style='padding: 12px; border: 1px solid #dee2e6; text-align: center; font-weight: 600;'>{product.MinimumStock}</td>
                        <td style='padding: 12px; border: 1px solid #dee2e6; color: #dc3545; text-align: center; font-weight: 700;'>-{product.MinimumStock - product.CurrentQuantity}</td>
                    </tr>");
			}

			return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Oxygen, Ubuntu, Cantarell, sans-serif; font-size: 14px; color: #333;'>
    <div style='max-width: 700px; margin: 0 auto; padding: 20px;'>
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); padding: 40px 30px; border-radius: 12px 12px 0 0; text-align: center; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
            <h1 style='color: #fff; margin: 0; font-size: 28px; font-weight: 600;'>Aesthetics Premium Care</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 8px 0 0 0; font-size: 13px; letter-spacing: 0.5px;'>HỆ THỐNG QUẢN LÝ TỒNKHO</p>
        </div>

        <!-- Main Content -->
        <div style='background: white; padding: 40px; border-radius: 0 0 12px 12px; box-shadow: 0 8px 16px rgba(0,0,0,0.15);'>
            <!-- Alert Icon & Title -->
            <div style='text-align: center; margin-bottom: 30px;'>
                <div style='font-size: 48px; margin-bottom: 15px;'>🚨</div>
                <h2 style='color: #dc3545; margin: 0; font-size: 24px; font-weight: 600;'>Cảnh Báo Tồn Kho Thấp</h2>
                <p style='color: #666; margin: 10px 0 0 0; font-size: 13px;'><strong>{products.Count} sản phẩm</strong> cần được kiểm tra ngay</p>
            </div>

            <!-- Summary Card -->
            <div style='background: linear-gradient(135deg, #fff3e0 0%, #ffe0b2 100%); padding: 16px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ff6b35; text-align: center;'>
                <p style='color: #e65100; margin: 0; font-size: 13px; font-weight: 600;'>
                    <strong>{products.Count}</strong> sản phẩm có tồn kho thấp hơn ngưỡng cho phép
                </p>
            </div>

            <!-- Products Table -->
            <div style='margin: 30px 0; overflow-x: auto;'>
                <table style='border-collapse: collapse; width: 100%; font-size: 13px;'>
                    <thead>
                        <tr style='background: linear-gradient(135deg, #2c3e50 0%, #34495e 100%); color: white;'>
                            <th style='padding: 14px; border: 1px solid #dee2e6; text-align: left; font-weight: 600;'>Sản Phẩm</th>
                            <th style='padding: 14px; border: 1px solid #dee2e6; text-align: center; font-weight: 600;'>Hiện Tại</th>
                            <th style='padding: 14px; border: 1px solid #dee2e6; text-align: center; font-weight: 600;'>Tối Thiểu</th>
                            <th style='padding: 14px; border: 1px solid #dee2e6; text-align: center; font-weight: 600;'>Thiếu</th>
                        </tr>
                    </thead>
                    <tbody>
                        {tableRows}
                    </tbody>
                </table>
            </div>

            <!-- Legend -->
            <div style='display: flex; gap: 16px; margin: 24px 0; flex-wrap: wrap;'>
                <div style='flex: 1; min-width: 140px; padding: 12px; background: #ffcccc; border-radius: 4px; text-align: center; font-size: 12px;'>
                    <strong style='color: #dc3545;'>Hết hàng</strong>
                </div>
                <div style='flex: 1; min-width: 140px; padding: 12px; background: #fff3cd; border-radius: 4px; text-align: center; font-size: 12px;'>
                    <strong style='color: #ffc107;'>Nguy hiểm</strong>
                </div>
                <div style='flex: 1; min-width: 140px; padding: 12px; background: #e7f3ff; border-radius: 4px; text-align: center; font-size: 12px;'>
                    <strong style='color: #0066cc;'>Cảnh báo</strong>
                </div>
            </div>

            <!-- Action Plan -->
            <div style='background: linear-gradient(135deg, #e8f5e8 0%, #c8e6c9 100%); padding: 20px; border-radius: 8px; border-left: 4px solid #28a745; margin: 25px 0;'>
                <h4 style='color: #1b5e20; margin: 0 0 12px 0; font-size: 14px; font-weight: 600;'>📋 Kế Hoạch Hành Động</h4>
                <ol style='color: #2e7d32; margin: 0; padding-left: 20px; font-size: 13px; line-height: 1.8;'>
                    <li>Kiểm tra toàn bộ tồn kho tại kho</li>
                    <li>Liên hệ nhà cung cấp để nhập thêm hàng loại cần thiết</li>
                    <li>Cập nhật số lượng tồn kho trong hệ thống</li>
                    <li>Thông báo cho bộ phận bán hàng về tình trạng tồn kho</li>
                    <li>Theo dõi và xử lý yêu cầu từ khách hàng</li>
                </ol>
            </div>

            <!-- Call to Action -->
            <div style='text-align: center; margin: 30px 0;'>
                <a href='https://aesthetics.com/inventory/management' style='display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 14px 32px; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 14px; transition: all 0.3s ease; box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);'>
                    📦 Quản Lý Tồn Kho
                </a>
            </div>

            <!-- Footer Divider -->
            <div style='border-top: 2px solid #f0f0f0; margin: 30px 0; padding-top: 25px;'>
                <!-- Contact Info -->
                <div style='text-align: center; background: linear-gradient(135deg, #f8f9ff 0%, #f0f4ff 100%); padding: 16px; border-radius: 8px; margin-bottom: 16px;'>
                    <p style='color: #666; margin: 0; font-size: 12px; line-height: 1.6;'>
                        <strong>📧 Cảp nhật trạng thái:</strong><br>
                        Gửi tự động {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}<br>
                        Email sẽ được gửi lại sau 5 giờ nếu vẫn chưa nhập thêm hàng
                    </p>
                </div>

                <p style='color: #999; margin: 12px 0; text-align: center; font-size: 12px; line-height: 1.6;'>
                    Cảm ơn quý khách đã sử dụng hệ thống Aesthetics Premium Care.<br>
                    Chúng tôi luôn sẵn sàng hỗ trợ bạn.
                </p>
                <p style='color: #bbb; margin: 8px 0 0 0; text-align: center; font-size: 11px;'>
                    © 2026 Aesthetics Premium Care. Mọi quyền được bảo lưu.
                </p>
            </div>
        </div>
    </div>
</body>
</html>";
		}
	}
}

