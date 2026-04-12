using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Aesthetics.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceRefundController : ControllerBase
    {
        private readonly IInvoicePaymentService _invoicePaymentService;
        private readonly ILogger<InvoiceRefundController> _logger;

        public InvoiceRefundController(
            IInvoicePaymentService invoicePaymentService,
            ILogger<InvoiceRefundController> logger)
        {
            _invoicePaymentService = invoicePaymentService;
            _logger = logger;
        }

        /// <summary>
        /// 🆕 Hoàn tiền cho hóa đơn
        /// POST: /api/invoicerefund/process-refund
        /// </summary>
        [HttpPost("process-refund")]
        public async Task<IActionResult> ProcessRefund([FromBody] RefundRequestModel request)
        {
            _logger.LogInformation(
                "ProcessRefund: InvoiceId: {InvoiceId}, RefundAmount: {Amount}, Reason: {Reason}",
                request.InvoiceId, request.RefundAmount, request.RefundReason);

            if (request == null || request.InvoiceId <= 0 || request.RefundAmount <= 0)
            {
                return BadRequest(new { message = "InvoiceId và RefundAmount phải > 0" });
            }

            var result = await _invoicePaymentService.ProcessRefund(
                request.InvoiceId,
                request.RefundAmount,
                request.RefundReason ?? "Hoàn hàng"
            );

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}