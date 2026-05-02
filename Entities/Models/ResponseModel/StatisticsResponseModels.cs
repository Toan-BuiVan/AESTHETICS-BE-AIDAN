using System;
using System.Collections.Generic;

namespace Aesthetics.Entities.Models.ResponseModel
{
	/// <summary>
	/// Response thống kê toàn diện theo khoảng thời gian
	/// </summary>
	public class MonthlyStatisticsResponse
	{
		/// <summary>Thời kỳ thống kê</summary>
		public DateTime StartDate { get; set; }

		/// <summary>Thời kỳ thống kê</summary>
		public DateTime EndDate { get; set; }

		/// <summary>Danh sách voucher sử dụng nhiều nhất</summary>
		public List<VoucherUsageStatistic> TopVouchersUsed { get; set; } = new();

		/// <summary>Danh sách bác sĩ có KPI tốt nhất</summary>
		public List<DoctorKPIStatistic> TopDoctorsWithBestKPI { get; set; } = new();

		/// <summary>Danh sách sản phẩm bán chạy nhất</summary>
		public List<ProductSalesStatistic> TopSellingProducts { get; set; } = new();

		/// <summary>Danh sách dịch vụ có nhiều người dùng nhất</summary>
		public List<ServicePopularityStatistic> TopPopularServices { get; set; } = new();

		/// <summary>Danh sách bác sĩ có đánh giá tốt nhất</summary>
		public List<DoctorRatingStatistic> TopDoctorsWithBestRatings { get; set; } = new();

		/// <summary>Danh sách nhân viên bán hàng tốt nhất</summary>
		public List<SalesStaffStatistic> TopSalesStaff { get; set; } = new();

		/// <summary>🆕 Thống kê thanh toán chi tiết</summary>
		public PaymentStatistics PaymentStats { get; set; } = new();

		/// <summary>🆕 Thống kê khách hàng mới vs khách hàng quay lại</summary>
		public CustomerStatistics CustomerStats { get; set; } = new();

		/// <summary>🆕 Thống kê hàng tồn kho</summary>
		public InventoryStatistics InventoryStats { get; set; } = new();

		/// <summary>🆕 Thống kê hóa đơn theo trạng thái</summary>
		public List<InvoiceStatusStatistic> InvoiceStatusStats { get; set; } = new();

		/// <summary>Thống kê tổng hợp</summary>
		public StatisticsSummary Summary { get; set; } = new();
	}

	/// <summary>
	/// 🆕 Thống kê thanh toán
	/// </summary>
	public class PaymentStatistics
	{
		/// <summary>Tổng doanh thu</summary>
		public decimal TotalRevenue { get; set; }

		/// <summary>Tổng số tiền đã thanh toán</summary>
		public decimal TotalPaidAmount { get; set; }

		/// <summary>Tổng số tiền còn nợ</summary>
		public decimal TotalOutstandingBalance { get; set; }

		/// <summary>Tỷ lệ thanh toán (%)</summary>
		public double PaymentRate { get; set; }

		/// <summary>Số hóa đơn đã thanh toán đầy đủ</summary>
		public int FullyPaidInvoices { get; set; }

		/// <summary>Số hóa đơn thanh toán một phần</summary>
		public int PartiallyPaidInvoices { get; set; }

		/// <summary>Số hóa đơn chưa thanh toán</summary>
		public int UnpaidInvoices { get; set; }

		/// <summary>Phương thức thanh toán phổ biến nhất</summary>
		public Dictionary<string, int> PaymentMethodStats { get; set; } = new();
	}

	/// <summary>
	/// 🆕 Thống kê khách hàng
	/// </summary>
	public class CustomerStatistics
	{
		/// <summary>Tổng khách hàng</summary>
		public int TotalCustomers { get; set; }

		/// <summary>Khách hàng mới (lần đầu mua)</summary>
		public int NewCustomers { get; set; }

		/// <summary>Khách hàng quay lại (mua nhiều lần)</summary>
		public int ReturningCustomers { get; set; }

		/// <summary>Khách hàng VIP (mua nhiều nhất)</summary>
		public int VIPCustomers { get; set; }

		/// <summary>Giá trị trung bình đơn hàng/khách</summary>
		public decimal AverageOrderValue { get; set; }

		/// <summary>Giá trị trung bình khách hàng (lifetime value)</summary>
		public decimal AverageCustomerLifetimeValue { get; set; }

		/// <summary>Top 5 khách hàng chi tiêu nhiều nhất</summary>
		public List<TopCustomerStatistic> TopCustomers { get; set; } = new();
	}

	/// <summary>
	/// 🆕 Khách hàng chi tiêu cao nhất
	/// </summary>
	public class TopCustomerStatistic
	{
		/// <summary>ID khách hàng</summary>
		public int CustomerId { get; set; }

		/// <summary>Tên khách hàng</summary>
		public string CustomerName { get; set; }

		/// <summary>Email</summary>
		public string Email { get; set; }

		/// <summary>Số điện thoại</summary>
		public string Phone { get; set; }

		/// <summary>Tổng chi tiêu</summary>
		public decimal TotalSpent { get; set; }

		/// <summary>Số lần mua</summary>
		public int PurchaseCount { get; set; }

		/// <summary>Trạng thái thành viên: Normal, Silver, Gold, Platinum</summary>
		public string MembershipStatus { get; set; }
	}

	/// <summary>
	/// 🆕 Thống kê hàng tồn kho
	/// </summary>
	public class InventoryStatistics
	{
		/// <summary>Tổng sản phẩm trong kho</summary>
		public int TotalProductsInStock { get; set; }

		/// <summary>Sản phẩm sắp hết (dưới mức tối thiểu)</summary>
		public int LowStockProducts { get; set; }

		/// <summary>Sản phẩm hết hàng</summary>
		public int OutOfStockProducts { get; set; }

		/// <summary>Giá trị tồn kho</summary>
		public decimal InventoryValue { get; set; }

		/// <summary>Danh sách sản phẩm sắp hết</summary>
		public List<LowStockProductStatistic> LowStockProductsList { get; set; } = new();
	}

	/// <summary>
	/// 🆕 Sản phẩm sắp hết hàng
	/// </summary>
	public class LowStockProductStatistic
	{
		/// <summary>ID sản phẩm</summary>
		public int ProductId { get; set; }

		/// <summary>Tên sản phẩm</summary>
		public string ProductName { get; set; }

		/// <summary>Số lượng hiện tại</summary>
		public int CurrentQuantity { get; set; }

		/// <summary>Số lượng tối thiểu</summary>
		public int MinimumStock { get; set; }

		/// <summary>Trạng thái</summary>
		public string Status { get; set; } // "LowStock" hoặc "OutOfStock"

		/// <summary>Giá vốn</summary>
		public decimal CostPrice { get; set; }

		/// <summary>Giá trị tồn kho</summary>
		public decimal StockValue { get; set; }
	}

	/// <summary>
	/// 🆕 Thống kê hóa đơn theo trạng thái
	/// </summary>
	public class InvoiceStatusStatistic
	{
		/// <summary>Trạng thái: ChuaThanhToan, ThanhToanMotPhan, DaThanhToan</summary>
		public string Status { get; set; }

		/// <summary>Số lượng hóa đơn</summary>
		public int Count { get; set; }

		/// <summary>Tổng tiền</summary>
		public decimal TotalAmount { get; set; }

		/// <summary>Phần trăm</summary>
		public double Percentage { get; set; }
	}

	/// <summary>
	/// Thống kê tổng hợp (cập nhật)</summary>
	public class StatisticsSummary
	{
		/// <summary>Tổng doanh thu</summary>
		public decimal TotalRevenue { get; set; }

		/// <summary>Tổng số hóa đơn</summary>
		public int TotalInvoices { get; set; }

		/// <summary>Tổng số khách hàng</summary>
		public int TotalCustomers { get; set; }

		/// <summary>Tổng voucher được sử dụng</summary>
		public int TotalVouchersUsed { get; set; }

		/// <summary>Tổng giảm giá từ voucher</summary>
		public decimal TotalVoucherDiscount { get; set; }

		/// <summary>Tổng số sản phẩm bán</summary>
		public int TotalProductsSold { get; set; }

		/// <summary>Tổng số dịch vụ cung cấp</summary>
		public int TotalServicesProvided { get; set; }

		/// <summary>Danh sách số bình luận/đánh giá tổng</summary>
		public int TotalComments { get; set; }

		/// <summary>Đánh giá trung bình</summary>
		public double AverageRating { get; set; }

		/// <summary>🆕 Doanh thu trung bình/ngày</summary>
		public decimal AverageDailyRevenue { get; set; }

		/// <summary>🆕 Hóa đơn trung bình/ngày</summary>
		public double AverageDailyInvoices { get; set; }

		/// <summary>🆕 Tổng hoa hồng (tất cả nhân viên)</summary>
		public decimal TotalCommission { get; set; }

		/// <summary>🆕 Tổng thưởng (tất cả nhân viên)</summary>
		public decimal TotalBonus { get; set; }
	}

	/// <summary>
	/// Thống kê voucher sử dụng
	/// </summary>
	public class VoucherUsageStatistic
	{
		/// <summary>ID Voucher</summary>
		public int VoucherId { get; set; }

		/// <summary>Mã voucher</summary>
		public string VoucherCode { get; set; }

		/// <summary>Số lần sử dụng</summary>
		public int UsageCount { get; set; }

		/// <summary>Tổng giảm giá</summary>
		public decimal TotalDiscount { get; set; }

		/// <summary>Giảm giá trung bình</summary>
		public decimal AverageDiscount { get; set; }

		/// <summary>Giá trị phần trăm giảm</summary>
		public decimal DiscountValue { get; set; }
	}

	/// <summary>
	/// Thống kê KPI bác sĩ (cập nhật)
	/// </summary>
	public class DoctorKPIStatistic
	{
		/// <summary>ID bác sĩ</summary>
		public int StaffId { get; set; }

		/// <summary>Tên bác sĩ</summary>
		public string FullName { get; set; }

		/// <summary>Email</summary>
		public string Email { get; set; }

		/// <summary>Số điện thoại</summary>
		public string Phone { get; set; }

		/// <summary>Chuyên khoa</summary>
		public string Specialization { get; set; }

		/// <summary>🆕 Hình ảnh bác sĩ</summary>
		public string StaffImage { get; set; }

		/// <summary>Số lịch hẹn thực hiện</summary>
		public int AppointmentCount { get; set; }

		/// <summary>Tổng hoa hồng</summary>
		public decimal TotalCommission { get; set; }

		/// <summary>Tổng thưởng</summary>
		public decimal TotalBonus { get; set; }

		/// <summary>Tổng doanh thu từ dịch vụ</summary>
		public decimal TotalServiceRevenue { get; set; }

		/// <summary>KPI score (hoa hồng + thưởng)</summary>
		public decimal KPIScore { get; set; }

		/// <summary>Điểm đánh giá trung bình</summary>
		public double AverageRating { get; set; }

		/// <summary>Số lượt đánh giá</summary>
		public int RatingCount { get; set; }
	}

	/// <summary>
	/// Thống kê bán sản phẩm
	/// </summary>
	public class ProductSalesStatistic
	{
		/// <summary>ID sản phẩm</summary>
		public int ProductId { get; set; }

		/// <summary>Tên sản phẩm</summary>
		public string ProductName { get; set; }

		/// <summary>Số lượng bán</summary>
		public int QuantitySold { get; set; }

		/// <summary>Doanh thu bán sản phẩm</summary>
		public decimal TotalRevenue { get; set; }

		/// <summary>Giá bán</summary>
		public decimal SellingPrice { get; set; }

		/// <summary>Giá vốn</summary>
		public decimal CostPrice { get; set; }

		/// <summary>Lợi nhuận</summary>
		public decimal Profit { get; set; }

		/// <summary>Tỷ suất lợi nhuận (%)</summary>
		public decimal ProfitMargin { get; set; }

		/// <summary>Số lần bình luận/đánh giá</summary>
		public int RatingCount { get; set; }

		/// <summary>Đánh giá trung bình</summary>
		public double AverageRating { get; set; }
	}

	/// <summary>
	/// Thống kê dịch vụ phổ biến
	/// </summary>
	public class ServicePopularityStatistic
	{
		/// <summary>ID dịch vụ</summary>
		public int ServiceId { get; set; }

		/// <summary>Tên dịch vụ</summary>
		public string ServiceName { get; set; }

		/// <summary>Số lượng khách hàng sử dụng</summary>
		public int CustomerCount { get; set; }

		/// <summary>Tổng lần sử dụng (buổi)</summary>
		public int TotalUsage { get; set; }

		/// <summary>Doanh thu</summary>
		public decimal TotalRevenue { get; set; }

		/// <summary>Giá dịch vụ</summary>
		public decimal ServicePrice { get; set; }

		/// <summary>Thời lượng (phút)</summary>
		public int Duration { get; set; }

		/// <summary>Số lần bình luận/đánh giá</summary>
		public int RatingCount { get; set; }

		/// <summary>Đánh giá trung bình</summary>
		public double AverageRating { get; set; }

		/// <summary>Loại: Dịch vụ đơn lẻ hay Liệu trình</summary>
		public bool IsCourse { get; set; }
	}

	/// <summary>
	/// Thống kê đánh giá bác sĩ (cập nhật)
	/// </summary>
	public class DoctorRatingStatistic
	{
		/// <summary>ID bác sĩ</summary>
		public int StaffId { get; set; }

		/// <summary>Tên bác sĩ</summary>
		public string FullName { get; set; }

		/// <summary>Email</summary>
		public string Email { get; set; }

		/// <summary>Chuyên khoa</summary>
		public string Specialization { get; set; }

		/// <summary>🆕 Hình ảnh bác sĩ</summary>
		public string StaffImage { get; set; }

		/// <summary>Số năm kinh nghiệm</summary>
		public int ExperienceYears { get; set; }

		/// <summary>Số lượt đánh giá</summary>
		public int RatingCount { get; set; }

		/// <summary>Đánh giá trung bình (1-5 sao)</summary>
		public double AverageRating { get; set; }

		/// <summary>Số sao 5</summary>
		public int FiveStarCount { get; set; }

		/// <summary>Số sao 4</summary>
		public int FourStarCount { get; set; }

		/// <summary>Số sao 3</summary>
		public int ThreeStarCount { get; set; }

		/// <summary>Số sao 2</summary>
		public int TwoStarCount { get; set; }

		/// <summary>Số sao 1</summary>
		public int OneStarCount { get; set; }

		/// <summary>Danh xưng: 'Thạc sĩ', 'Tiến sĩ'</summary>
		public string Degree { get; set; }

		/// <summary>Số giấy phép hành nghề</summary>
		public string LicenseNumber { get; set; }

		/// <summary>Số lịch hẹn thực hiện</summary>
		public int AppointmentCount { get; set; }
	}

	/// <summary>
	/// Thống kê nhân viên bán hàng
	/// </summary>
	public class SalesStaffStatistic
	{
		/// <summary>ID nhân viên</summary>
		public int StaffId { get; set; }

		/// <summary>Tên nhân viên</summary>
		public string FullName { get; set; }

		/// <summary>Email</summary>
		public string Email { get; set; }

		/// <summary>Số điện thoại</summary>
		public string Phone { get; set; }

		/// <summary>Số hóa đơn tạo ra</summary>
		public int InvoiceCount { get; set; }

		/// <summary>Tổng doanh thu bán hàng</summary>
		public decimal TotalRevenue { get; set; }

		/// <summary>Tổng hoa hồng</summary>
		public decimal TotalCommission { get; set; }

		/// <summary>Tổng thưởng</summary>
		public decimal TotalBonus { get; set; }

		/// <summary>Điểm bán hàng</summary>
		public int SalesPoints { get; set; }

		/// <summary>Số sản phẩm bán</summary>
		public int ProductsSoldCount { get; set; }

		/// <summary>Số dịch vụ bán</summary>
		public int ServicesSoldCount { get; set; }

		/// <summary>Trạng thái: Active, Probation, Resigned, Leave</summary>
		public int EmploymentStatus { get; set; }
	}
}