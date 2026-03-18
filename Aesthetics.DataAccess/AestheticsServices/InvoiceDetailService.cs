using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
	/// <summary>
	/// Service xử lý nghiệp vụ liên quan đến chi tiết hóa đơn (InvoiceDetail)
	/// Bao gồm: Lấy danh sách chi tiết hóa đơn theo InvoiceId
	/// </summary>
	public class InvoiceDetailService : IInvoiceDetailService
	{
		#region Dependencies - Các dependency được inject vào service

		private readonly ILogger<InvoiceDetailService> _logger;
		private readonly IInvoiceDetailsRepository _invoiceDetailsRepository;   // Repository xử lý bảng InvoiceDetails
		private readonly IInvoiceRepository _invoiceRepository;                 // Repository xử lý bảng Invoices (để validate InvoiceId)

		#endregion

		#region Constructor - Khởi tạo service với dependency injection

		public InvoiceDetailService(
			ILogger<InvoiceDetailService> logger,
			IInvoiceDetailsRepository invoiceDetailsRepository,
			IInvoiceRepository invoiceRepository)
		{
			_logger = logger;
			_invoiceDetailsRepository = invoiceDetailsRepository;
			_invoiceRepository = invoiceRepository;
		}

		#endregion

		#region Public Methods - Các phương thức công khai của service

		/// <summary>
		/// LẤY DANH SÁCH CHI TIẾT HÓA ĐƠN THEO INVOICE ID
		/// LUỒNG XỬ LÝ:
		/// 1. Validate InvoiceId (kiểm tra > 0 và hóa đơn có tồn tại không)
		/// 2. Xây dựng điều kiện lọc (theo InvoiceId và chưa bị xóa)
		/// 3. Lấy tất cả chi tiết hóa đơn thuộc InvoiceId đó
		/// 4. Sắp xếp theo thứ tự tạo và trả về kết quả
		/// 5. Trả về danh sách trong BaseDataCollection (không cần phân trang vì thường ít records)
		/// </summary>
		/// <param name="invoiceId">ID của hóa đơn cần lấy chi tiết</param>
		/// <returns>Danh sách chi tiết hóa đơn với thông tin đầy đủ</returns>
		public async Task<BaseDataCollection<InvoiceDetailEntity>> getlist(int invoiceId)
		{
			try
			{
				_logger.LogInformation("Bắt đầu lấy danh sách chi tiết hóa đơn cho InvoiceId: {InvoiceId}", invoiceId);

				// BƯỚC 1: Validate InvoiceId - phải là số dương và hóa đơn phải tồn tại
				if (!await ValidateInvoiceIdAsync(invoiceId))
				{
					_logger.LogWarning("InvoiceId không hợp lệ: {InvoiceId}", invoiceId);
					return CreateEmptyResult();
				}

				// BƯỚC 2: Xây dựng điều kiện lọc
				// Lấy tất cả chi tiết hóa đơn thuộc InvoiceId đó và chưa bị xóa
				Expression<Func<InvoiceDetailEntity, bool>> predicate = x =>
					x.InvoiceId == invoiceId && !x.DeleteStatus;

				// BƯỚC 3: Lấy tất cả chi tiết hóa đơn thỏa mãn điều kiện
				var invoiceDetails = await _invoiceDetailsRepository.FindByPredicate(predicate);
				var totalCount = invoiceDetails.Count();

				// BƯỚC 4: Sắp xếp kết quả theo thứ tự ưu tiên
				// Sắp xếp theo: ID tăng dần (thứ tự tạo)
				var sortedData = invoiceDetails
					.OrderBy(x => x.Id)                          // Thứ tự tạo (ID nhỏ hơn tạo trước)
					.ThenBy(x => x.ProductId ?? x.ServiceId)     // Nếu cùng thời gian thì sắp theo Product/Service
					.ToList();

				// BƯỚC 5: Tạo kết quả trả về
				// Không cần phân trang vì chi tiết hóa đơn thường ít (1-10 items)
				var result = new BaseDataCollection<InvoiceDetailEntity>
				{
					BaseDatas = sortedData,                      // Danh sách chi tiết hóa đơn
					TotalRecordCount = totalCount,               // Tổng số bản ghi
					PageIndex = 1,                               // Trang đầu tiên (không phân trang)
					PageCount = 1                                // Chỉ có 1 trang
				};

				// BƯỚC 6: Ghi log thông tin kết quả và các thống kê
				LogInvoiceDetailsStatistics(invoiceId, sortedData, totalCount);

				_logger.LogInformation("Lấy danh sách chi tiết hóa đơn thành công: InvoiceId {InvoiceId}, Tìm thấy {Count} chi tiết",
					invoiceId, totalCount);

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi ngoại lệ khi lấy danh sách chi tiết hóa đơn: InvoiceId {InvoiceId}", invoiceId);
				// Trả về kết quả rỗng thay vì throw exception
				return CreateEmptyResult();
			}
		}

		#endregion

		#region Private Helper Methods - Các phương thức hỗ trợ nội bộ

		/// <summary>
		/// VALIDATE INVOICE ID
		/// Kiểm tra tính hợp lệ của InvoiceId:
		/// - Phải là số dương (> 0)
		/// - Hóa đơn phải tồn tại trong database
		/// - Hóa đơn không được bị xóa (DeleteStatus = false)
		/// </summary>
		/// <param name="invoiceId">ID cần validate</param>
		/// <returns>True nếu hợp lệ, False nếu không hợp lệ</returns>
		private async Task<bool> ValidateInvoiceIdAsync(int invoiceId)
		{
			// Kiểm tra InvoiceId phải là số dương
			if (invoiceId <= 0)
			{
				_logger.LogWarning("Validate InvoiceId thất bại: ID phải là số dương. InvoiceId: {InvoiceId}", invoiceId);
				return false;
			}

			try
			{
				// Kiểm tra hóa đơn có tồn tại trong database không
				var invoice = await _invoiceRepository.GetById(invoiceId);
				if (invoice == null)
				{
					_logger.LogWarning("Validate InvoiceId thất bại: Hóa đơn không tồn tại. InvoiceId: {InvoiceId}", invoiceId);
					return false;
				}

				// Kiểm tra hóa đơn có bị xóa không
				if (invoice.DeleteStatus)
				{
					_logger.LogWarning("Validate InvoiceId thất bại: Hóa đơn đã bị xóa. InvoiceId: {InvoiceId}", invoiceId);
					return false;
				}

				_logger.LogInformation("Validate InvoiceId thành công: InvoiceId {InvoiceId} hợp lệ", invoiceId);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi ngoại lệ khi validate InvoiceId: {InvoiceId}", invoiceId);
				return false;
			}
		}

		/// <summary>
		/// TẠO KẾT QUẢ RỖNG KHI CÓ LỖI
		/// Tạo BaseDataCollection rỗng để trả về khi có lỗi xảy ra
		/// </summary>
		/// <returns>BaseDataCollection rỗng</returns>
		private BaseDataCollection<InvoiceDetailEntity> CreateEmptyResult()
		{
			return new BaseDataCollection<InvoiceDetailEntity>
			{
				BaseDatas = new List<InvoiceDetailEntity>(),    // Danh sách rỗng
				TotalRecordCount = 0,                           // Không có bản ghi nào
				PageIndex = 0,                                  // Không có trang
				PageCount = 0                                   // Không có trang
			};
		}

		/// <summary>
		/// GHI LOG THỐNG KÊ CHI TIẾT HÓA ĐƠN
		/// Phân tích và ghi log các thông tin thống kê về chi tiết hóa đơn:
		/// - Số lượng sản phẩm vs dịch vụ
		/// - Tổng tiền từng loại
		/// - Số lượng có voucher/giảm giá
		/// </summary>
		/// <param name="invoiceId">ID hóa đơn</param>
		/// <param name="details">Danh sách chi tiết hóa đơn</param>
		/// <param name="totalCount">Tổng số chi tiết</param>
		private void LogInvoiceDetailsStatistics(int invoiceId, List<InvoiceDetailEntity> details, int totalCount)
		{
			try
			{
				if (!details.Any())
				{
					_logger.LogInformation("Thống kê chi tiết hóa đơn: InvoiceId {InvoiceId} - Không có chi tiết nào", invoiceId);
					return;
				}

				// Thống kê theo loại item
				var productCount = details.Count(x => x.ProductId.HasValue);      // Số lượng sản phẩm
				var serviceCount = details.Count(x => x.ServiceId.HasValue);      // Số lượng dịch vụ
				var treatmentCount = details.Count(x => x.TreatmentPlanId.HasValue); // Số lượng gói liệu trình

				// Thống kê tiền tệ
				var totalMoney = details.Sum(x => x.TotalMoney ?? 0);             // Tổng tiền tất cả
				var totalDiscount = details.Sum(x => x.DiscountValue);            // Tổng tiền giảm giá
				var voucherCount = details.Count(x => x.VoucherId.HasValue);      // Số chi tiết có voucher

				// Thống kê trạng thái
				var paidCount = details.Count(x => x.Status == "DaThanhToan");    // Đã thanh toán
				var pendingCount = details.Count(x => x.Status == "DangCho");     // Đang chờ

				// Ghi log thống kê tổng hợp
				_logger.LogInformation(
					"Thống kê chi tiết hóa đơn InvoiceId {InvoiceId}: " +
					"Tổng {TotalCount} chi tiết | " +
					"Sản phẩm: {ProductCount} | Dịch vụ: {ServiceCount} | Gói liệu trình: {TreatmentCount} | " +
					"Tổng tiền: {TotalMoney:C} | Giảm giá: {TotalDiscount:C} | " +
					"Có voucher: {VoucherCount} | Đã TT: {PaidCount} | Đang chờ: {PendingCount}",
					invoiceId, totalCount, productCount, serviceCount, treatmentCount,
					totalMoney, totalDiscount, voucherCount, paidCount, pendingCount);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Lỗi khi ghi log thống kê cho InvoiceId {InvoiceId}", invoiceId);
			}
		}

		#endregion
	}
}

