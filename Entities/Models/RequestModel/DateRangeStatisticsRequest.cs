using System;

namespace Aesthetics.Entities.Models.RequestModel
{
    /// <summary>
    /// Request cho thống kê theo khoảng thời gian
    /// </summary>
    public class DateRangeStatisticsRequest
    {
        /// <summary>Ngày bắt đầu (tháng/năm)</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Ngày kết thúc (tháng/năm)</summary>
        public DateTime EndDate { get; set; }

        /// <summary>Số bản ghi tối đa cần trả về (mặc định 10)</summary>
        public int TopCount { get; set; } = 10;
    }
}