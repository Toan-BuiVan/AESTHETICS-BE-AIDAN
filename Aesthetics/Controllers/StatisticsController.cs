using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using ASP_NetCore_Aesthetics.Filter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Aesthetics.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        #region Dependencies

        private readonly IStatisticsService _statisticsService;

        #endregion

        #region Constructor

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

		#endregion

		#region Public Endpoints

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getmonthlystatistics")]
		[HttpPost("getmonthlystatistics")]
        public async Task<IActionResult> GetMonthlyStatistics([FromBody] DateRangeStatisticsRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Request không hợp lệ"
                    });
                }

                if (request.StartDate >= request.EndDate)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "StartDate phải nhỏ hơn EndDate"
                    });
                }

                // Validate date range (không quá 1 năm)
                var dateDifference = (request.EndDate - request.StartDate).TotalDays;
                if (dateDifference > 365)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Khoảng thời gian không được vượt quá 365 ngày"
                    });
                }

                var result = await _statisticsService.GetMonthlyStatisticsAsync(request);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Lấy thống kê thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy thống kê: " + ex.Message
                });
            }
        }

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("gettopvouchersused")]
		[HttpPost("gettopvouchersused")]
        public async Task<IActionResult> GetTopVouchersUsed([FromBody] DateRangeStatisticsRequest request)
        {
            try
            {
                if (request == null || request.StartDate >= request.EndDate)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu đầu vào không hợp lệ"
                    });
                }

                var result = await _statisticsService.GetTopVouchersUsedAsync(request);

                return Ok(new
                {
                    success = true,
                    data = result,
                    count = result.Count,
                    message = "Lấy danh sách voucher thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("gettopdoctorsbykpi")]
		[HttpPost("gettopdoctorsbykpi")]
        public async Task<IActionResult> GetTopDoctorsByKPI([FromBody] DateRangeStatisticsRequest request)
        {
            try
            {
                if (request == null || request.StartDate >= request.EndDate)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu đầu vào không hợp lệ"
                    });
                }

                var result = await _statisticsService.GetTopDoctorsByKPIAsync(request);

                return Ok(new
                {
                    success = true,
                    data = result,
                    count = result.Count,
                    message = "Lấy danh sách bác sĩ KPI tốt nhất thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("gettopsellingproducts")]
		[HttpPost("gettopsellingproducts")]
        public async Task<IActionResult> GetTopSellingProducts([FromBody] DateRangeStatisticsRequest request)
        {
            try
            {
                if (request == null || request.StartDate >= request.EndDate)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu đầu vào không hợp lệ"
                    });
                }

                var result = await _statisticsService.GetTopSellingProductsAsync(request);

                return Ok(new
                {
                    success = true,
                    data = result,
                    count = result.Count,
                    message = "Lấy danh sách sản phẩm bán chạy nhất thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("gettoppopularservices")]
		[HttpPost("gettoppopularservices")]
        public async Task<IActionResult> GetTopPopularServices([FromBody] DateRangeStatisticsRequest request)
        {
            try
            {
                if (request == null || request.StartDate >= request.EndDate)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu đầu vào không hợp lệ"
                    });
                }

                var result = await _statisticsService.GetTopPopularServicesAsync(request);

                return Ok(new
                {
                    success = true,
                    data = result,
                    count = result.Count,
                    message = "Lấy danh sách dịch vụ phổ biến nhất thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("gettopdoctorsbyrating")]
		[HttpPost("gettopdoctorsbyrating")]
        public async Task<IActionResult> GetTopDoctorsByRating([FromBody] DateRangeStatisticsRequest request)
        {
            try
            {
                if (request == null || request.StartDate >= request.EndDate)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu đầu vào không hợp lệ"
                    });
                }

                var result = await _statisticsService.GetTopDoctorsByRatingAsync(request);

                return Ok(new
                {
                    success = true,
                    data = result,
                    count = result.Count,
                    message = "Lấy danh sách bác sĩ đánh giá tốt nhất thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("gettopsalesstaff")]
		[HttpPost("gettopsalesstaff")]
        public async Task<IActionResult> GetTopSalesStaff([FromBody] DateRangeStatisticsRequest request)
        {
            try
            {
                if (request == null || request.StartDate >= request.EndDate)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu đầu vào không hợp lệ"
                    });
                }

                var result = await _statisticsService.GetTopSalesStaffAsync(request);

                return Ok(new
                {
                    success = true,
                    data = result,
                    count = result.Count,
                    message = "Lấy danh sách nhân viên bán hàng tốt nhất thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getstatisticssummary")]
		[HttpPost("getstatisticssummary")]
        public async Task<IActionResult> GetStatisticsSummary([FromBody] DateRangeStatisticsRequest request)
        {
            try
            {
                if (request == null || request.StartDate >= request.EndDate)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu đầu vào không hợp lệ"
                    });
                }

                var result = await _statisticsService.GetStatisticsSummaryAsync(request);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Lấy thống kê tổng hợp thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("daily-revenue")]
		[HttpPost("daily-revenue")]
        public async Task<IActionResult> GetDailyRevenueStatistics([FromBody] DailyRevenueStatisticsRequest request)
        {
            try
            {
                // ✅ Validate request
                if (request == null || request.Month < 1 || request.Month > 12)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Tháng phải từ 1 đến 12",
                        data = (object)null
                    });
                }

                if (request.Year < 2000 || request.Year > DateTime.Now.Year + 10)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Năm không hợp lệ",
                        data = (object)null
                    });
                }

                // ✅ Gọi service
                var result = await _statisticsService.GetDailyRevenueStatisticsAsync(request);

                return Ok(new
                {
                    success = true,
                    message = $"Thống kê doanh thu theo ngày tháng {request.Month}/{request.Year} thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server khi lấy thống kê doanh thu",
                    error = ex.Message,
                    data = (object)null
                });
            }
        }

        #endregion
    }
}