using System;

namespace Aesthetics.Entities.Models.ResponseModel
{
    /// <summary>
    /// ✅ Response thống kê doanh thu theo ngày
    /// </summary>
    public class DailyRevenueStatisticResponse
    {
        /// <summary>
        /// Ngày (1-31)
        /// </summary>
        public int Day { get; set; }

        /// <summary>
        /// Tháng (1-12)
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// Năm
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Định dạng ngày: "2026-05-10"
        /// </summary>
        public string DateString { get; set; }

        /// <summary>
        /// Định dạng đầy đủ: "10/05/2026"
        /// </summary>
        public string FormattedDate { get; set; }

        /// <summary>
        /// Tổng tiền hóa đơn đã thanh toán trong ngày
        /// Status: DaThanhToan hoặc ThanhToanMotPhan (có PaidAmount > 0)
        /// </summary>
        public decimal PaidRevenue { get; set; }

        /// <summary>
        /// Tổng tiền hóa đơn chưa thanh toán trong ngày
        /// Status: ChuaThanhToan hoặc ThanhToanMotPhan (có OutstandingBalance > 0)
        /// </summary>
        public decimal UnpaidRevenue { get; set; }

        /// <summary>
        /// Tổng doanh thu trong ngày (Paid + Unpaid)
        /// </summary>
        public decimal TotalRevenue => PaidRevenue + UnpaidRevenue;

        /// <summary>
        /// Số hóa đơn đã thanh toán
        /// </summary>
        public int PaidInvoiceCount { get; set; }

        /// <summary>
        /// Số hóa đơn chưa thanh toán
        /// </summary>
        public int UnpaidInvoiceCount { get; set; }

        /// <summary>
        /// Tổng số hóa đơn trong ngày
        /// </summary>
        public int TotalInvoiceCount => PaidInvoiceCount + UnpaidInvoiceCount;
    }

    /// <summary>
    /// ✅ Response wrapper cho thống kê doanh thu theo tháng
    /// Chứa 30 bản ghi (tương ứng 1 tháng)
    /// </summary>
    public class DailyRevenueStatisticsResponse
    {
        /// <summary>
        /// Tháng thống kê
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// Năm thống kê
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Định dạng tháng năm: "05/2026"
        /// </summary>
        public string MonthYearString { get; set; }

        /// <summary>
        /// Danh sách thống kê theo ngày (30 bản ghi cho 1 tháng)
        /// </summary>
        public List<DailyRevenueStatisticResponse> DailyStatistics { get; set; }

        /// <summary>
        /// Tổng doanh thu đã thanh toán trong tháng
        /// </summary>
        public decimal TotalPaidRevenue { get; set; }

        /// <summary>
        /// Tổng doanh thu chưa thanh toán trong tháng
        /// </summary>
        public decimal TotalUnpaidRevenue { get; set; }

        /// <summary>
        /// Tổng doanh thu trong tháng
        /// </summary>
        public decimal TotalMonthlyRevenue => TotalPaidRevenue + TotalUnpaidRevenue;

        /// <summary>
        /// Tổng số hóa đơn đã thanh toán trong tháng
        /// </summary>
        public int TotalPaidInvoices { get; set; }

        /// <summary>
        /// Tổng số hóa đơn chưa thanh toán trong tháng
        /// </summary>
        public int TotalUnpaidInvoices { get; set; }

        /// <summary>
        /// Tổng số hóa đơn trong tháng
        /// </summary>
        public int TotalInvoices => TotalPaidInvoices + TotalUnpaidInvoices;
    }
}