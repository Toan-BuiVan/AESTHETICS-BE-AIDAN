using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
    public interface IInventoryAlertService
    {
		/// <summary>
		/// Tạo cảnh báo tồn kho thấp
		/// </summary>
		Task<bool> CreateLowStockAlert(int productId, int currentQuantity, int minimumStock);

		/// <summary>
		/// Gửi email cảnh báo cho tất cả alerts chờ gửi
		/// </summary>
		Task<bool> SendPendingAlertEmails();

		/// <summary>
		/// Dọn dẹp alerts cũ
		/// </summary>
		Task<bool> CleanupOldAlerts();
	}
}
