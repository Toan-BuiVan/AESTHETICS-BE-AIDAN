using Aesthetics.Data.AestheticsInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aesthetics.Data.BackgroundServices
{
	public class InventoryAlertBackgroundService : BackgroundService
	{
		private readonly ILogger<InventoryAlertBackgroundService> _logger;
		private readonly IServiceProvider _serviceProvider;
		private readonly TimeSpan _period = TimeSpan.FromHours(5); // Gửi email mỗi 5 giờ

		public InventoryAlertBackgroundService(
			ILogger<InventoryAlertBackgroundService> logger,
			IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("InventoryAlertBackgroundService started");

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					using var scope = _serviceProvider.CreateScope();
					var inventoryAlertService = scope.ServiceProvider.GetRequiredService<IInventoryAlertService>();

					// Gửi email cảnh báo cho các alerts chờ gửi
					await inventoryAlertService.SendPendingAlertEmails();

					// Dọn dẹp alerts cũ (chạy 1 lần mỗi ngày)
					if (DateTime.UtcNow.Hour == 2) // Chạy vào 2h sáng
					{
						await inventoryAlertService.CleanupOldAlerts();
					}

					_logger.LogInformation("InventoryAlertBackgroundService: Completed cycle at {Time}", DateTime.UtcNow);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "InventoryAlertBackgroundService: Error during execution");
				}

				// Chờ 5 giờ trước khi chạy lại
				await Task.Delay(_period, stoppingToken);
			}

			_logger.LogInformation("InventoryAlertBackgroundService stopped");
		}
	}
}
