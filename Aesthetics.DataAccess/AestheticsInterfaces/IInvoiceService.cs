using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
	/// <summary>
	/// Interface quản lý hóa đơn (Invoice)
	/// Bao gồm: Tạo hóa đơn, lấy danh sách, cập nhật trạng thái thanh toán
	/// </summary>
	public interface IInvoiceService
	{
		Task<bool> create(CreateInvoice invoice);
		Task<BaseDataCollection<InvoiceDetailFullResponseModel>>? GetInvoiceDetails(GetInvoice invoiceId);


		#region Update Payment Status - Cập nhật trạng thái thanh toán

		/// <summary>
		/// CẬP NHẬT TRẠNG THÁI THANH TOÁN CỦA HÓA ĐƠN (Tự động tính toán)
		/// 
		/// LUỒNG XỬ LÝ:
		/// 1. Validate dữ liệu đầu vào (ID hóa đơn, số tiền)
		/// 2. Lấy hóa đơn từ database, kiểm tra tồn tại
		/// 3. Cộng thêm số tiền thanh toán vào PaidAmount hiện tại
		/// 4. Kiểm tra không vượt quá tổng tiền của hóa đơn
		/// 5. Tính toán OutstandingBalance (số tiền còn nợ)
		/// 6. TỰ ĐỘNG TÍNH TOÁN STATUS thanh toán:
		///    - ChuaThanhToan: Khi PaidAmount = 0
		///    - ThanhToanMotPhan: Khi 0 < PaidAmount < TotalMoney
		///    - DaThanhToan: Khi PaidAmount >= TotalMoney
		/// 7. Cập nhật phương thức thanh toán (nếu có)
		/// 8. Lưu hóa đơn vào database
		/// </summary>
		Task<bool> UpdatePaymentStatus(UpdateInvoicePaymentStatus request);

		/// <summary>
		/// Cập nhật trạng thái giao hàng của hóa đơn
		/// </summary>
		Task<bool> UpdateInvoiceOrderStatus(updateinvoiceorderstatus updateinvoiceorderstatus);

		#endregion
	}
}
