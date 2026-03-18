using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices.Common;
using Aesthetics.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.RepositoryServices
{
	public class InventoryAlertRepository : CommonRepository<InventoryAlertEntity>, IInventoryAlertRepository
	{
		public InventoryAlertRepository(ILogger<CommonRepository<InventoryAlertEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}

		/// <summary>
		/// Lấy alert chưa gửi email và đã quá 5h từ lần gửi cuối
		/// </summary>
		public async Task<IEnumerable<InventoryAlertEntity>> GetPendingEmailAlerts()
		{
			try
			{
				var fiveHoursAgo = DateTime.UtcNow.AddHours(-5);

				return await _dbContext.Set<InventoryAlertEntity>()
					.Where(a => !a.DeleteStatus &&
							   (!a.IsEmailSent ||
								(a.EmailSentDate.HasValue && a.EmailSentDate.Value <= fiveHoursAgo)))
					.Include(a => a.Product)
					.ToListAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetPendingEmailAlerts: Exception occurred");
				return new List<InventoryAlertEntity>();
			}
		}

		/// <summary>
		/// Kiểm tra xem đã có alert cho sản phẩm này chưa (trong vòng 24h gần nhất)
		/// </summary>
		public async Task<bool> HasRecentAlertForProduct(int productId)
		{
			try
			{
				var twentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);

				return await _dbContext.Set<InventoryAlertEntity>()
					.AnyAsync(a => !a.DeleteStatus &&
								  a.ProductId == productId &&
								  a.CreatedDate >= twentyFourHoursAgo);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "HasRecentAlertForProduct: Exception for ProductId {ProductId}", productId);
				return false;
			}
		}

		/// <summary>
		/// Đánh dấu alert đã gửi email
		/// </summary>
		public async Task<bool> MarkEmailSent(int alertId)
		{
			try
			{
				var alert = await _dbContext.Set<InventoryAlertEntity>()
					.FirstOrDefaultAsync(a => a.Id == alertId && !a.DeleteStatus);

				if (alert == null)
				{
					_logger.LogWarning("MarkEmailSent: Alert not found with Id {AlertId}", alertId);
					return false;
				}

				alert.IsEmailSent = true;
				alert.EmailSentDate = DateTime.UtcNow;
				alert.EmailSentCount++;

				_dbContext.Set<InventoryAlertEntity>().Update(alert);
				var result = await _dbContext.SaveChangesAsync();

				return result > 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "MarkEmailSent: Exception for AlertId {AlertId}", alertId);
				return false;
			}
		}

		/// <summary>
		/// Xóa các alert cũ (quá 30 ngày)
		/// </summary>
		public async Task<bool> DeleteOldAlerts()
		{
			try
			{
				var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

				var oldAlerts = await _dbContext.Set<InventoryAlertEntity>()
					.Where(a => !a.DeleteStatus && a.CreatedDate <= thirtyDaysAgo)
					.ToListAsync();

				if (!oldAlerts.Any())
				{
					_logger.LogInformation("DeleteOldAlerts: No old alerts to delete");
					return true;
				}

				foreach (var alert in oldAlerts)
				{
					alert.DeleteStatus = true;
				}

				_dbContext.Set<InventoryAlertEntity>().UpdateRange(oldAlerts);
				var result = await _dbContext.SaveChangesAsync();

				_logger.LogInformation("DeleteOldAlerts: Marked {Count} old alerts as deleted", oldAlerts.Count);
				return result > 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "DeleteOldAlerts: Exception occurred");
				return false;
			}
		}
	}
}
