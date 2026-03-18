using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.EmailService;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Microsoft.Extensions.Logging;

namespace Aesthetics.Data.AestheticsServices
{
	public class InventoryAlertService : IInventoryAlertService
	{
		private readonly ILogger<InventoryAlertService> _logger;
		private readonly IInventoryAlertRepository _inventoryAlertRepository;
		private readonly IProductRepository _productRepository;
		private readonly IEmailService _emailService;

		public InventoryAlertService(
			ILogger<InventoryAlertService> logger,
			IInventoryAlertRepository inventoryAlertRepository,
			IProductRepository productRepository,
			IEmailService emailService)
		{
			_logger = logger;
			_inventoryAlertRepository = inventoryAlertRepository;
			_productRepository = productRepository;
			_emailService = emailService;
		}

		/// <summary>
		/// Tạo cảnh báo tồn kho thấp
		/// </summary>
		public async Task<bool> CreateLowStockAlert(int productId, int currentQuantity, int minimumStock)
		{
			try
			{
				// Kiểm tra xem đã có alert gần đây cho sản phẩm này chưa
				var hasRecentAlert = await _inventoryAlertRepository.HasRecentAlertForProduct(productId);
				if (hasRecentAlert)
				{
					_logger.LogInformation("CreateLowStockAlert: Recent alert already exists for ProductId {ProductId}", productId);
					return true; 
				}

				var alert = new InventoryAlertEntity
				{
					ProductId = productId,
					CurrentQuantity = currentQuantity,
					MinimumStock = minimumStock,
					IsEmailSent = false,
					CreatedDate = DateTime.UtcNow,
					DeleteStatus = false
				};

				var created = await _inventoryAlertRepository.CreateEntity(alert);
				if (created)
				{
					_logger.LogInformation("CreateLowStockAlert: Created alert for ProductId {ProductId}, Current: {Current}, Minimum: {Minimum}",
						productId, currentQuantity, minimumStock);
				}

				return created;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CreateLowStockAlert: Exception for ProductId {ProductId}", productId);
				return false;
			}
		}

		/// <summary>
		/// Gửi email cảnh báo cho tất cả alerts chờ gửi
		/// </summary>
		public async Task<bool> SendPendingAlertEmails()
		{
			try
			{
				var pendingAlerts = await _inventoryAlertRepository.GetPendingEmailAlerts();
				if (!pendingAlerts.Any())
				{
					_logger.LogInformation("SendPendingAlertEmails: No pending alerts to send");
					return true;
				}

				// Nhóm alerts theo sản phẩm và chuẩn bị danh sách gửi email
				var alertsToSend = new List<(string ProductName, int CurrentQuantity, int MinimumStock)>();
				var alertIds = new List<int>();

				foreach (var alert in pendingAlerts)
				{
					var product = await _productRepository.GetById(alert.ProductId);
					if (product != null)
					{
						alertsToSend.Add((product.ProductName ?? "Unknown", alert.CurrentQuantity, alert.MinimumStock));
						alertIds.Add(alert.Id);
					}
				}

				if (!alertsToSend.Any())
				{
					_logger.LogWarning("SendPendingAlertEmails: No valid products found for alerts");
					return false;
				}

				// Gửi email tổng hợp (có thể cấu hình email admin từ appsettings)
				string adminEmail = "admin@aesthetics.com"; // TODO: Lấy từ configuration
				var emailSent = await _emailService.SendBulkLowStockAlert(adminEmail, alertsToSend);

				if (emailSent)
				{
					// Đánh dấu tất cả alerts đã gửi email
					foreach (var alertId in alertIds)
					{
						await _inventoryAlertRepository.MarkEmailSent(alertId);
					}

					_logger.LogInformation("SendPendingAlertEmails: Sent email for {Count} alerts", alertIds.Count);
				}

				return emailSent;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SendPendingAlertEmails: Exception");
				return false;
			}
		}

		/// <summary>
		/// Dọn dẹp alerts cũ
		/// </summary>
		public async Task<bool> CleanupOldAlerts()
		{
			try
			{
				var cleaned = await _inventoryAlertRepository.DeleteOldAlerts();
				if (cleaned)
				{
					_logger.LogInformation("CleanupOldAlerts: Successfully cleaned up old alerts");
				}
				return cleaned;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CleanupOldAlerts: Exception");
				return false;
			}
		}
	}
}
