using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
	public interface IRefundServcie
	{
		public Task<bool> createrefundservice(CreateRefundModel model);
		public Task<bool> updaterefundservice(UpdtaeRefundModel model);
		public Task<BaseDataCollection<RefundEntity>> getlistrefund(getlist model);

		/// <summary>
		/// ✅ Cộng điểm rating khi thanh toán hóa đơn thành công
		/// Công thức: 10 điểm / 1,000,000 VNĐ
		/// </summary>
		Task<bool> AddRatingPointsAsync(int customerId, decimal invoiceAmount);

		/// <summary>
		/// ✅ Trừ điểm rating khi hoàn tiền thành công
		/// Công thức: 10 điểm / 1,000,000 VNĐ
		/// </summary>
		Task<bool> SubtractRatingPointsAsync(int customerId, decimal refundAmount);

		/// <summary>
		/// 🆕 Trừ điểm bán hàng nhân viên khi khách hàng hủy hóa đơn
		/// Công thức: 1 điểm = 20,000 VNĐ (tính dựa trên số tiền thanh toán đã được cộng điểm)
		/// </summary>
		Task<bool> SubtractSalesPointsForStaffAsync(int staffId, decimal paidAmount);
	}
}
