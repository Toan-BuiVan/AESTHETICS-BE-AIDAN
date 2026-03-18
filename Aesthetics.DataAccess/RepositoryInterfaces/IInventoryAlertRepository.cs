using Aesthetics.Data.RepositoryInterfaces.Common;
using Aesthetics.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.RepositoryInterfaces
{
    public interface IInventoryAlertRepository : ICommonRepository<InventoryAlertEntity>
	{
		/// <summary>
		/// Lấy alert chưa gửi email và đã quá 5h từ lần gửi cuối
		/// </summary>
		Task<IEnumerable<InventoryAlertEntity>> GetPendingEmailAlerts();

		/// <summary>
		/// Kiểm tra xem đã có alert cho sản phẩm này chưa (trong vòng 24h gần nhất)
		/// </summary>
		Task<bool> HasRecentAlertForProduct(int productId);

		/// <summary>
		/// Đánh dấu alert đã gửi email
		/// </summary>
		Task<bool> MarkEmailSent(int alertId);

		/// <summary>
		/// Xóa các alert cũ (quá 30 ngày)
		/// </summary>
		Task<bool> DeleteOldAlerts();
	}
}
