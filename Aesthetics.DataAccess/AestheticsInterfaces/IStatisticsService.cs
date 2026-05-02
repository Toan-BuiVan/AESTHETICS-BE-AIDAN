using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsInterfaces
{
	/// <summary>
	/// Service thống kê doanh số, hiệu suất, bán hàng theo khoảng thời gian
	/// Statistics Service for monthly/quarterly/yearly reporting
	/// </summary>
	public interface IStatisticsService
	{
		/// <summary>
		/// Lấy thống kê toàn diện theo khoảng thời gian
		/// Get comprehensive statistics for a date range
		/// </summary>
		/// <param name="request">Yêu cầu thống kê có StartDate và EndDate</param>
		/// <returns>Thống kê chi tiết</returns>
		Task<MonthlyStatisticsResponse> GetMonthlyStatisticsAsync(DateRangeStatisticsRequest request);

		/// <summary>
		/// Lấy thống kê voucher sử dụng nhiều nhất
		/// </summary>
		Task<List<VoucherUsageStatistic>> GetTopVouchersUsedAsync(DateRangeStatisticsRequest request);

		/// <summary>
		/// Lấy thống kê bác sĩ có KPI tốt nhất
		/// </summary>
		Task<List<DoctorKPIStatistic>> GetTopDoctorsByKPIAsync(DateRangeStatisticsRequest request);

		/// <summary>
		/// Lấy thống kê sản phẩm bán chạy nhất
		/// </summary>
		Task<List<ProductSalesStatistic>> GetTopSellingProductsAsync(DateRangeStatisticsRequest request);

		/// <summary>
		/// Lấy thống kê dịch vụ có nhiều người dùng nhất
		/// </summary>
		Task<List<ServicePopularityStatistic>> GetTopPopularServicesAsync(DateRangeStatisticsRequest request);

		/// <summary>
		/// Lấy thống kê bác sĩ có đánh giá tốt nhất
		/// </summary>
		Task<List<DoctorRatingStatistic>> GetTopDoctorsByRatingAsync(DateRangeStatisticsRequest request);

		/// <summary>
		/// Lấy thống kê nhân viên bán hàng tốt nhất
		/// </summary>
		Task<List<SalesStaffStatistic>> GetTopSalesStaffAsync(DateRangeStatisticsRequest request);

		/// <summary>
		/// Lấy thống kê tổng hợp
		/// </summary>
		Task<StatisticsSummary> GetStatisticsSummaryAsync(DateRangeStatisticsRequest request);

		/// <summary>
		/// 🆕 Lấy thống kê thanh toán chi tiết
		/// </summary>
		Task<PaymentStatistics> GetPaymentStatisticsAsync(DateRangeStatisticsRequest request);

		/// <summary>
		/// 🆕 Lấy thống kê khách hàng
		/// </summary>
		Task<CustomerStatistics> GetCustomerStatisticsAsync(DateRangeStatisticsRequest request);

		/// <summary>
		/// 🆕 Lấy thống kê hàng tồn kho
		/// </summary>
		Task<InventoryStatistics> GetInventoryStatisticsAsync(DateRangeStatisticsRequest request);

		/// <summary>
		/// 🆕 Lấy thống kê hóa đơn theo trạng thái thanh toán
		/// </summary>
		Task<List<InvoiceStatusStatistic>> GetInvoiceStatusStatisticsAsync(DateRangeStatisticsRequest request);
	}
}