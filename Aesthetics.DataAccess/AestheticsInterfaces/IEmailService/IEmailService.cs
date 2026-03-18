using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces.EmailService
{
	public interface IEmailService
	{
		/// <summary>
		/// Gửi email cảnh báo tồn kho thấp
		/// </summary>
		Task<bool> SendLowStockAlert(string toEmail, string productName, int currentQuantity, int minimumStock);

		/// <summary>
		/// Gửi email tổng hợp nhiều sản phẩm thiếu tồn kho
		/// </summary>
		Task<bool> SendBulkLowStockAlert(string toEmail, List<(string ProductName, int CurrentQuantity, int MinimumStock)> products);

		/// <summary>
		/// Gửi email xác nhận lịch hẹn khi đặt thành công
		/// </summary>
		Task<bool> SendAppointmentConfirmation(string customerEmail, string customerName, string serviceName, DateTime appointmentTime, string staffName);

		/// <summary>
		/// Gửi email nhắc nhở lịch hẹn sắp đến
		/// </summary>
		Task<bool> SendAppointmentReminder(string customerEmail, string customerName, string serviceName, DateTime appointmentTime, string staffName);

		/// <summary>
		/// Gửi email thông báo hủy lịch hẹn
		/// </summary>
		Task<bool> SendAppointmentCancellation(string customerEmail, string customerName, string serviceName, DateTime appointmentTime, string staffName);
	}
}

