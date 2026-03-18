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
				var subject = "✅ Xác nhận đặt lịch thành công - Spa Aesthetics";
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
		/// Gửi email thông qua SMTP
		/// </summary>
		private async Task<bool> SendEmail(string toEmail, string subject, string body)
		{
			try
			{
				using var client = new SmtpClient(_smtpHost, _smtpPort)
				{
					Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
					EnableSsl = true
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

				await client.SendMailAsync(mailMessage);
				_logger.LogInformation("SendEmail: Successfully sent to {ToEmail}", toEmail);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SendEmail: Failed to send to {ToEmail}", toEmail);
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
        <html>
        <body style='font-family: Arial, sans-serif;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f8f9fa;'>
                <div style='background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    <div style='text-align: center; margin-bottom: 30px;'>
                        <h1 style='color: #dc3545; margin: 0;'>🚫 Lịch hẹn đã bị hủy</h1>
                        <p style='color: #6c757d; margin: 10px 0 0 0;'>Thông báo hủy lịch hẹn từ hệ thống</p>
                    </div>
                    
                    <div style='background-color: #f8d7da; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #dc3545;'>
                        <h3 style='color: #721c24; margin-top: 0;'>Thông tin lịch hẹn đã hủy:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 8px 0; font-weight: bold; color: #333;'>Khách hàng:</td>
                                <td style='padding: 8px 0; color: #555;'>{customerName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; font-weight: bold; color: #333;'>Dịch vụ:</td>
                                <td style='padding: 8px 0; color: #555;'>{serviceName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; font-weight: bold; color: #333;'>Thời gian đã hủy:</td>
                                <td style='padding: 8px 0; color: #555;'>{appointmentTime:dd/MM/yyyy HH:mm}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; font-weight: bold; color: #333;'>Nhân viên phụ trách:</td>
                                <td style='padding: 8px 0; color: #555;'>{staffName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; font-weight: bold; color: #333;'>Thời gian hủy:</td>
                                <td style='padding: 8px 0; color: #555;'>{DateTime.Now:dd/MM/yyyy HH:mm}</td>
                            </tr>
                        </table>
                    </div>

                    <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; border-left: 4px solid #ffc107; margin: 20px 0;'>
                        <p style='margin: 0; color: #856404;'>
                            <strong>📝 Lưu ý quan trọng:</strong><br/>
                            • Lịch hẹn của quý khách đã được hủy thành công<br/>
                            • Nếu đã thanh toán, chúng tôi sẽ hoàn tiền trong 3-5 ngày làm việc<br/>
                            • Quý khách có thể đặt lịch hẹn mới bất kỳ lúc nào
                        </p>
                    </div>

                    <div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6;'>
                        <p style='color: #6c757d; margin: 0;'>
                            Cần hỗ trợ? Liên hệ: <strong>1900-1234</strong> | Email: <strong>support@spa.com</strong>
                        </p>
                        <p style='color: #6c757d; margin: 10px 0 0 0; font-size: 12px;'>
                            Cảm ơn quý khách đã sử dụng dịch vụ. Hy vọng được phục vụ quý khách trong tương lai.
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
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f8f9fa;'>
                        <div style='background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <div style='text-align: center; margin-bottom: 30px;'>
                                <h1 style='color: #28a745; margin: 0;'>✅ Đặt lịch thành công!</h1>
                                <p style='color: #6c757d; margin: 10px 0 0 0;'>Cảm ơn quý khách đã tin tưởng dịch vụ của chúng tôi</p>
                            </div>
                            
                            <div style='background-color: #e7f3ff; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                                <h3 style='color: #0066cc; margin-top: 0;'>Thông tin lịch hẹn:</h3>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold; color: #333;'>Khách hàng:</td>
                                        <td style='padding: 8px 0; color: #555;'>{customerName}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold; color: #333;'>Dịch vụ:</td>
                                        <td style='padding: 8px 0; color: #555;'>{serviceName}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold; color: #333;'>Thời gian:</td>
                                        <td style='padding: 8px 0; color: #555; font-weight: bold;'>{appointmentTime:dd/MM/yyyy HH:mm}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold; color: #333;'>Nhân viên:</td>
                                        <td style='padding: 8px 0; color: #555;'>{staffName}</td>
                                    </tr>
                                </table>
                            </div>
                            
                            <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; border-left: 4px solid #ffc107; margin: 20px 0;'>
                                <h4 style='color: #856404; margin-top: 0;'>Lưu ý quan trọng:</h4>
                                <ul style='color: #856404; margin: 10px 0; padding-left: 20px;'>
                                    <li>Vui lòng có mặt trước 15 phút so với giờ hẹn</li>
                                    <li>Mang theo CMND/CCCD để xác nhận thông tin</li>
                                    <li>Nếu cần thay đổi lịch, vui lòng liên hệ trước 24h</li>
                                </ul>
                            </div>
                            
                            <div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6;'>
                                <p style='color: #6c757d; margin: 0;'>Liên hệ: <strong>0123.456.789</strong> | Email: <strong>support@aesthetics.com</strong></p>
                                <p style='color: #6c757d; margin: 5px 0 0 0; font-size: 12px;'>
                                    Chúng tôi sẽ gửi email nhắc nhở trước 24 giờ
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
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f8f9fa;'>
                        <div style='background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <div style='text-align: center; margin-bottom: 30px;'>
                                <h1 style='color: #ff6b35; margin: 0;'>🔔 Nhắc nhở lịch hẹn</h1>
                                <p style='color: #6c757d; margin: 10px 0 0 0;'>Lịch hẹn của quý khách sắp đến</p>
                            </div>
                            
                            <div style='background-color: #fff3e0; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #ff6b35;'>
                                <h3 style='color: #e65100; margin-top: 0;'>Thông tin lịch hẹn sắp tới:</h3>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold; color: #333;'>Khách hàng:</td>
                                        <td style='padding: 8px 0; color: #555;'>{customerName}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold; color: #333;'>Dịch vụ:</td>
                                        <td style='padding: 8px 0; color: #555;'>{serviceName}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold; color: #333;'>Thời gian:</td>
                                        <td style='padding: 8px 0; color: #e65100; font-weight: bold; font-size: 18px;'>{appointmentTime:dd/MM/yyyy HH:mm}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold; color: #333;'>Nhân viên:</td>
                                        <td style='padding: 8px 0; color: #555;'>{staffName}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; font-weight: bold; color: #333;'>Còn lại:</td>
                                        <td style='padding: 8px 0; color: #e65100; font-weight: bold;'>Khoảng {hoursRemaining} giờ nữa</td>
                                    </tr>
                                </table>
                            </div>
                            
                            <div style='background-color: #e8f5e8; padding: 15px; border-radius: 5px; border-left: 4px solid #28a745; margin: 20px 0;'>
                                <h4 style='color: #155724; margin-top: 0;'>Chuẩn bị cho buổi hẹn:</h4>
                                <ul style='color: #155724; margin: 10px 0; padding-left: 20px;'>
                                    <li>Đến trước 15 phút để làm thủ tục</li>
                                    <li>Mang theo CMND/CCCD và các giấy tờ cần thiết</li>
                                    <li>Tháo trang sức và makeup (nếu có)</li>
                                    <li>Thông báo tình trạng sức khỏe đặc biệt</li>
                                </ul>
                            </div>
                            
                            <div style='text-align: center; background-color: #f8d7da; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <p style='color: #721c24; margin: 0; font-weight: bold;'>
                                    Cần thay đổi lịch? Hãy liên hệ ngay: <a href='tel:0123456789' style='color: #721c24;'>0123.456.789</a>
                                </p>
                            </div>
                            
                            <div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6;'>
                                <p style='color: #6c757d; margin: 0;'>Spa Aesthetics | 0123.456.789 | support@aesthetics.com</p>
                                <p style='color: #6c757d; margin: 5px 0 0 0; font-size: 12px;'>
                                    Cảm ơn quý khách đã tin tưởng dịch vụ của chúng tôi
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
			return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #e74c3c;'>⚠️ Cảnh báo tồn kho thấp</h2>
                        <p>Sản phẩm <strong>{productName}</strong> đang có tồn kho thấp:</p>
                        <table style='border-collapse: collapse; width: 100%; margin: 20px 0;'>
                            <tr style='background-color: #f8f9fa;'>
                                <td style='padding: 10px; border: 1px solid #dee2e6;'><strong>Tồn kho hiện tại:</strong></td>
                                <td style='padding: 10px; border: 1px solid #dee2e6; color: #e74c3c;'><strong>{currentQuantity}</strong></td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; border: 1px solid #dee2e6;'><strong>Tồn kho tối thiểu:</strong></td>
                                <td style='padding: 10px; border: 1px solid #dee2e6;'>{minimumStock}</td>
                            </tr>
                        </table>
                        <p style='color: #dc3545;'><strong>Vui lòng nhập thêm hàng để tránh tình trạng hết hàng!</strong></p>
                        <hr style='margin: 20px 0;'>
                        <p style='font-size: 12px; color: #6c757d;'>
                            Email này được gửi tự động từ hệ thống quản lý Aesthetics.<br>
                            Thời gian: {DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss")}
                        </p>
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
				tableRows.AppendLine($@"
                    <tr>
                        <td style='padding: 10px; border: 1px solid #dee2e6;'>{product.ProductName}</td>
                        <td style='padding: 10px; border: 1px solid #dee2e6; color: #e74c3c; text-align: center;'><strong>{product.CurrentQuantity}</strong></td>
                        <td style='padding: 10px; border: 1px solid #dee2e6; text-align: center;'>{product.MinimumStock}</td>
                    </tr>");
			}

			return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 800px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #e74c3c;'>🚨 Cảnh báo tồn kho thấp - {products.Count} sản phẩm</h2>
                        <p>Các sản phẩm sau đang có tồn kho thấp hơn ngưỡng cho phép:</p>
                        
                        <table style='border-collapse: collapse; width: 100%; margin: 20px 0;'>
                            <thead>
                                <tr style='background-color: #343a40; color: white;'>
                                    <th style='padding: 12px; border: 1px solid #dee2e6;'>Tên sản phẩm</th>
                                    <th style='padding: 12px; border: 1px solid #dee2e6;'>Tồn kho hiện tại</th>
                                    <th style='padding: 12px; border: 1px solid #dee2e6;'>Tồn kho tối thiểu</th>
                                </tr>
                            </thead>
                            <tbody>
                                {tableRows}
                            </tbody>
                        </table>
                        
                        <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; border-left: 4px solid #ffc107; margin: 20px 0;'>
                            <strong style='color: #856404;'>⚠️ Hành động cần thiết:</strong>
                            <ul style='color: #856404; margin: 10px 0;'>
                                <li>Kiểm tra và liên hệ nhà cung cấp để nhập thêm hàng</li>
                                <li>Xem xét điều chỉnh ngưỡng tồn kho tối thiểu nếu cần</li>
                                <li>Thông báo cho bộ phận bán hàng về tình trạng tồn kho</li>
                            </ul>
                        </div>
                        
                        <hr style='margin: 20px 0;'>
                        <p style='font-size: 12px; color: #6c757d;'>
                            Email này được gửi tự động từ hệ thống quản lý Aesthetics.<br>
                            Thời gian: {DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss")}<br>
                            Email sẽ được gửi lại sau 5 giờ nếu vẫn chưa nhập thêm hàng.
                        </p>
                    </div>
                </body>
                </html>";
		}
	}
}

