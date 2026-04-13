using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Aesthetics.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceRefundController : ControllerBase
    {
        private readonly ILogger<InvoiceRefundController> _logger;
        private readonly IInvoicePaymentService _invoicePaymentService;

        public InvoiceRefundController(
            ILogger<InvoiceRefundController> logger,
            IInvoicePaymentService invoicePaymentService)
        {
            _logger = logger;
            _invoicePaymentService = invoicePaymentService;
        }

        /// <summary>
        /// ✅ Hoàn hàng và hoàn tiền cho khách hàng
        /// POST: api/invoicerefund/process-refund
        /// </summary>
        [HttpPost("process-refund")]
        public async Task<IActionResult> ProcessRefund([FromBody] RefundRequestModel request)
        {
            try
            {
                _logger.LogInformation("REFUND_API_START: Nhận request hoàn hàng - InvoiceId: {InvoiceId}, Amount: {Amount:C}",
                    request.InvoiceId, request.RefundAmount);

                var result = await _invoicePaymentService.ProcessRefund(
                    request.InvoiceId,
                    request.RefundAmount,
                    request.RefundReason);

                _logger.LogInformation("REFUND_API_RESPONSE: {Success} - {Message}",
                    result.Success, result.Message);

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "REFUND_API_EXCEPTION: Lỗi khi xử lý hoàn hàng");
                return StatusCode(500, new { success = false, message = "Lỗi server khi xử lý hoàn hàng" });
            }
        }
    }
}