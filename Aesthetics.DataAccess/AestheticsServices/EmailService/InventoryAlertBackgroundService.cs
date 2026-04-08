using Aesthetics.Data.AestheticsInterfaces.EmailService;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Enum;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices.EmailService
{
	/// <summary>
	/// 🆕 Background Service để cảnh báo khi tồn kho sản phẩm <= MinimumStock
	/// Gửi email đến staff có IsDoctor = false (Nhân viên quản lý kho)
	/// Chạy định kỳ mỗi 6 giờ
	/// </summary>
	public class InventoryAlertBackgroundService : BackgroundService
	{
		private readonly ILogger<InventoryAlertBackgroundService> _logger;
		private readonly IServiceProvider _serviceProvider;

		public InventoryAlertBackgroundService(
			ILogger<InventoryAlertBackgroundService> logger,
			IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("🏭 InventoryAlertBackgroundService started");

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await ProcessInventoryAlerts();
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "🔴 InventoryAlertBackgroundService: Exception occurred");
				}

				// Chạy mỗi 2 giờ (có thể chỉnh thành 1, 2, 3 giờ tùy ý)
				await Task.Delay(TimeSpan.FromHours(2), stoppingToken);
			}
		}

		/// <summary>
		/// 🔍 Tìm các sản phẩm có tồn kho <= MinimumStock và gửi cảnh báo
		/// </summary>
		private async Task ProcessInventoryAlerts()
		{
			using var scope = _serviceProvider.CreateScope();
			var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
			var staffRepository = scope.ServiceProvider.GetRequiredService<IStaffRepository>();
			var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

			try
			{
				_logger.LogInformation("📋 ProcessInventoryAlerts: Starting inventory check");

				// ✅ BƯỚC 1: Lấy tất cả sản phẩm không bị xóa
				var products = await productRepository.FindByPredicate(x => !x.DeleteStatus);

				if (!products.Any())
				{
					_logger.LogInformation("📦 ProcessInventoryAlerts: No products found");
					return;
				}

				// ✅ BƯỚC 2: Lọc sản phẩm có tồn kho <= MinimumStock
				var lowStockProducts = products
					.Where(p => p.Quantity <= p.MinimumStock && p.MinimumStock > 0)
					.ToList();

				if (!lowStockProducts.Any())
				{
					_logger.LogInformation("✅ ProcessInventoryAlerts: No low stock products");
					return;
				}

				_logger.LogInformation("⚠️ ProcessInventoryAlerts: Found {Count} products with low stock", lowStockProducts.Count());

				// ✅ BƯỚC 3: Lấy tất cả staff không phải bác sĩ (IsDoctor = false)
				var warehouseStaff = await staffRepository.FindByPredicate(x =>
					!x.DeleteStatus &&
					x.IsDoctor != true &&  // Không phải bác sĩ
					!string.IsNullOrEmpty(x.Email)  // Có email hợp lệ
				);

				if (!warehouseStaff.Any())
				{
					_logger.LogWarning("⚠️ ProcessInventoryAlerts: No warehouse staff found with valid email");
					return;
				}

				_logger.LogInformation("👥 ProcessInventoryAlerts: Found {Count} warehouse staff", warehouseStaff.Count());

				// ✅ BƯỚC 4: Gửi email cảnh báo tồn kho cho mỗi nhân viên kho
				foreach (var staff in warehouseStaff)
				{
					try
					{
						await SendInventoryAlertEmail(staff, lowStockProducts, emailService);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "🔴 ProcessInventoryAlerts: Failed to send alert to StaffId {StaffId}", staff.Id);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "🔴 ProcessInventoryAlerts: Exception occurred");
			}
		}

		/// <summary>
		/// 📧 Gửi email cảnh báo tồn kho cho nhân viên
		/// </summary>
		private async Task SendInventoryAlertEmail(
			Aesthetics.Entities.Entities.StaffEntity staff,
			List<Aesthetics.Entities.Entities.ProductEntity> lowStockProducts,
			IEmailService emailService)
		{
			try
			{
				if (string.IsNullOrEmpty(staff.Email))
				{
					_logger.LogWarning("📧 SendInventoryAlertEmail: Staff has no email. StaffId {StaffId}", staff.Id);
					return;
				}

				_logger.LogInformation("📧 Sending inventory alert to {Email} (StaffId: {StaffId})", staff.Email, staff.Id);

				// ✅ Chuyển đổi sang format tuple mà interface yêu cầu
				var productTuples = lowStockProducts
					.Select(p => (p.ProductName, p.Quantity, p.MinimumStock))
					.ToList();

				// ✅ Gửi email bằng phương thức SendBulkLowStockAlert
				var emailSent = await emailService.SendBulkLowStockAlert(
					toEmail: staff.Email,
					products: productTuples
				);

				if (emailSent)
				{
					_logger.LogInformation("✅ SendInventoryAlertEmail: Successfully sent to {Email}", staff.Email);
				}
				else
				{
					_logger.LogWarning("⚠️ SendInventoryAlertEmail: Failed to send to {Email}", staff.Email);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "🔴 SendInventoryAlertEmail: Exception for StaffId {StaffId}", staff.Id);
			}
		}

		/// <summary>
		/// 📝 Xây dựng nội dung email HTML
		/// </summary>
		private string BuildInventoryAlertEmailBody(string staffName, List<Aesthetics.Entities.Entities.ProductEntity> products)
		{
			var productRows = string.Join("\n", products.Select(p => $@"
				<tr>
					<td style='padding: 8px; border: 1px solid #ddd;'>{p.ProductName}</td>
					<td style='padding: 8px; border: 1px solid #ddd; text-align: center;'>{p.Quantity}</td>
					<td style='padding: 8px; border: 1px solid #ddd; text-align: center;'>{p.MinimumStock}</td>
					<td style='padding: 8px; border: 1px solid #ddd; text-align: center; color: red; font-weight: bold;'>{p.MinimumStock - p.Quantity}</td>
				</tr>
			"));

			return $@"
<!DOCTYPE html>
<html>
<head>
	<meta charset='UTF-8'>
	<style>
		body {{ font-family: Arial, sans-serif; color: #333; }}
		.container {{ max-width: 900px; margin: 0 auto; padding: 20px; }}
		.header {{ background-color: #ff6b6b; color: white; padding: 20px; border-radius: 5px; margin-bottom: 20px; }}
		.header h1 {{ margin: 0; font-size: 24px; }}
		.alert-section {{ background-color: #fff3cd; padding: 15px; border-left: 4px solid #ff6b6b; margin-bottom: 20px; }}
		table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
		th {{ background-color: #f8f9fa; padding: 12px; text-align: left; border: 1px solid #ddd; font-weight: bold; }}
		td {{ padding: 10px; border: 1px solid #ddd; }}
		.footer {{ color: #666; font-size: 12px; margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; }}
		.action-btn {{ display: inline-block; background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; margin-top: 10px; }}
	</style>
</head>
<body>
	<div class='container'>
		<div class='header'>
			<h1>🚨 Cảnh báo Tồn Kho Thấp</h1>
		</div>

		<p>Xin chào <strong>{staffName}</strong>,</p>

		<div class='alert-section'>
			<strong>⚠️ Lưu ý:</strong> Hệ thống phát hiện <strong>{products.Count} sản phẩm</strong> có số lượng tồn kho <= mức tối thiểu (MinimumStock).
		</div>

		<h3>📦 Danh sách sản phẩm cần xử lý:</h3>
		
		<table>
			<thead>
				<tr>
					<th>Tên Sản Phẩm</th>
					<th>Tồn Kho Hiện Tại</th>
					<th>Mức Tối Thiểu</th>
					<th>Cần Bổ Sung</th>
				</tr>
			</thead>
			<tbody>
				{productRows}
			</tbody>
		</table>

		<p><strong>📝 Hành động cần thực hiện:</strong></p>
		<ul>
			<li>Kiểm tra số lượng sản phẩm trên hệ thống</li>
			<li>Liên hệ nhà cung cấp để đặt hàng bổ sung nếu cần</li>
			<li>Cập nhật lại dữ liệu tồn kho</li>
		</ul>

		<p><strong>⏰ Thời gian kiểm tra:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>

		<div class='footer'>
			<p>Đây là email tự động từ hệ thống quản lý phòng khám thẩm mỹ.<br/>
			Vui lòng không trả lời email này.</p>
		</div>
	</div>
</body>
</html>
			";
		}
	}
}
