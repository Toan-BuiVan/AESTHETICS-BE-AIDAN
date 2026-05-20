using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using ASP_NetCore_Aesthetics.Filter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Aesthetics.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RefundController : ControllerBase
    {
        private readonly ILogger<RefundController> _logger;
        private readonly IRefundServcie _refundService;

        public RefundController(
            ILogger<RefundController> logger,
            IRefundServcie refundService)
        {
            _logger = logger;
            _refundService = refundService;
        }

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("createrefund")]
		[HttpPost("createrefund")]
        public async Task<IActionResult> CreateRefund([FromBody] CreateRefundModel request)
        {
            try
            {
                _logger.LogInformation("CREATE_REFUND_API_START: Tạo yêu cầu hoàn tiền - InvoiceId: {InvoiceId}, CustomerId: {CustomerId}, Reason: {Reason}",
                    request?.InvoiceId, request?.CustomerId, request?.RefundReason);

                // ✅ Gọi service để tạo
                var result = await _refundService.createrefundservice(request);

                if (result)
                {
                    _logger.LogInformation("CREATE_REFUND_API_SUCCESS: Tạo yêu cầu hoàn tiền thành công - InvoiceId: {InvoiceId}, CustomerId: {CustomerId}",
                        request.InvoiceId, request.CustomerId);

                    return Ok(new
                    {
                        success = true,
                        message = "Tạo yêu cầu hoàn tiền thành công",
                        data = (object)null
                    });
                }
                else
                {
                    _logger.LogError("CREATE_REFUND_API_FAILED: Tạo yêu cầu hoàn tiền thất bại - InvoiceId: {InvoiceId}",
                        request.InvoiceId);

                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Tạo yêu cầu hoàn tiền thất bại (hóa đơn chưa thanh toán hoặc đã có yêu cầu đang chờ)",
                        data = (object)null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CREATE_REFUND_API_EXCEPTION: Lỗi khi tạo yêu cầu hoàn tiền");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server khi tạo yêu cầu hoàn tiền",
                    error = ex.Message,
                    data = (object)null
                });
            }
        }

		[ServiceFilter(typeof(Filter_CheckToken))]
		[Filter_Authorization("updatestatusrefund")]
		[HttpPost("updatestatusrefund")]
        public async Task<IActionResult> UpdateRefundStatus([FromBody] UpdtaeRefundModel request)
        {
            try
            {
                _logger.LogInformation("UPDATE_REFUND_API_START: Cập nhật trạng thái hoàn tiền - RefundId: {RefundId}, Status: {Status}",
                    request?.Id, request?.Status);

                // ✅ Gọi service để cập nhật
                var result = await _refundService.updaterefundservice(request);

                if (result)
                {
                    _logger.LogInformation("UPDATE_REFUND_API_SUCCESS: Cập nhật trạng thái hoàn tiền thành công - RefundId: {RefundId}, Status: {Status}",
                        request.Id, request.Status);

                    return Ok(new
                    {
                        success = true,
                        message = $"Cập nhật trạng thái hoàn tiền thành công (Trạng thái: {request.Status})",
                        data = (object)null
                    });
                }
                else
                {
                    _logger.LogError("UPDATE_REFUND_API_FAILED: Cập nhật trạng thái hoàn tiền thất bại - RefundId: {RefundId}",
                        request.Id);

                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Cập nhật trạng thái hoàn tiền thất bại",
                        data = (object)null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UPDATE_REFUND_API_EXCEPTION: Lỗi khi cập nhật trạng thái hoàn tiền");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server khi cập nhật trạng thái hoàn tiền",
                    error = ex.Message,
                    data = (object)null
                });
            }
        }

		//[ServiceFilter(typeof(Filter_CheckToken))]
		//[Filter_Authorization("getlistrefund")]
		[HttpPost("getlistrefund")]
        public async Task<IActionResult> GetRefundList([FromBody] getlist request)
        {
            try
            {
                _logger.LogInformation("GET_LIST_REFUND_API_START: Lấy danh sách hoàn tiền - InvoiceId: {InvoiceId}, CustomerId: {CustomerId}, PageNo: {PageNo}, PageSize: {PageSize}",
                    request?.InvoiceId, request?.CustomerId, request?.PageNo, request?.PageSize);

                // ✅ Gọi service để lấy danh sách
                var result = await _refundService.getlistrefund(request);

                _logger.LogInformation("GET_LIST_REFUND_API_SUCCESS: Lấy danh sách hoàn tiền thành công - TotalCount: {TotalCount}, PageNo: {PageNo}, PageSize: {PageSize}, TotalPages: {TotalPages}",
                    result.TotalRecordCount, result.PageIndex, request.PageSize, result.PageCount);

                return Ok(new
                {
                    success = result.BaseDatas != null && result.BaseDatas.Count > 0,
                    message = result.BaseDatas != null && result.BaseDatas.Count > 0
                        ? "Lấy danh sách yêu cầu hoàn tiền thành công"
                        : "Không tìm thấy yêu cầu hoàn tiền nào",
                    data = result.BaseDatas,
                    pagination = new
                    {
                        totalRecords = result.TotalRecordCount,
                        pageIndex = result.PageIndex,
                        pageSize = request.PageSize,
                        totalPages = result.PageCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET_LIST_REFUND_API_EXCEPTION: Lỗi khi lấy danh sách hoàn tiền");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server khi lấy danh sách hoàn tiền",
                    error = ex.Message,
                    data = (object)null
                });
            }
        }
    }
}