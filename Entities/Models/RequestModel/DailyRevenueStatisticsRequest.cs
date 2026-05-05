using System;

namespace Aesthetics.Entities.Models.RequestModel
{
    /// <summary>
    /// ✅ Request thống kê doanh thu theo ngày
    /// Nhận vào: Tháng, Năm
    /// </summary>
    public class DailyRevenueStatisticsRequest
    {
        /// <summary>
        /// Tháng (1-12)
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// Năm (ví dụ: 2026)
        /// </summary>
        public int Year { get; set; }
    }
}