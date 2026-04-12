using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using ASP_NetCore_Aesthetics.Services.VnPaySevices;
using ASP_NetCore_Aesthetics.Services.MomoServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ASP_NetCore_Aesthetics.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicePaymentController : ControllerBase
    {
        private readonly IInvoicePaymentService _invoicePaymentService;
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;
        private readonly ILogger<InvoicePaymentController> _logger;

        public InvoicePaymentController(
            IInvoicePaymentService invoicePaymentService,
            IVnPayService vnPayService,
            IMomoService momoService,
            ILogger<InvoicePaymentController> logger)
        {
            _invoicePaymentService = invoicePaymentService;
            _vnPayService = vnPayService;
            _momoService = momoService;
            _logger = logger;
        }

        [HttpPost("vnpay/create-payment-url")]
        public async Task<IActionResult> CreateVnPayPaymentUrl([FromBody] CreateVnPayPaymentUrlRequest request)
        {
            try
            {
                _logger.LogInformation("CREATE_VNPAY_PAYMENT_URL: Tạo URL thanh toán VNPay - InvoiceId: {InvoiceId}", request.InvoiceId);

                if (request?.InvoiceId <= 0)
                {
                    _logger.LogWarning("CREATE_VNPAY_PAYMENT_URL: InvoiceId không hợp lệ - InvoiceId: {InvoiceId}", request?.InvoiceId);
                    return BadRequest(new
                    {
                        success = false,
                        message = "InvoiceId không hợp lệ",
                        data = (object)null
                    });
                }

                var paymentModel = await _invoicePaymentService.GenerateVnPayPaymentUrl(request.InvoiceId, HttpContext);
                if (paymentModel == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Không thể tạo URL thanh toán. Hóa đơn không tồn tại, đã thanh toán hoặc đã bị xóa",
                        data = (object)null
                    });
                }

                var paymentUrl = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext);

                if (string.IsNullOrEmpty(paymentUrl))
                {
                    _logger.LogError("CREATE_VNPAY_PAYMENT_URL: Payment URL rỗng");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Không thể tạo URL thanh toán",
                        data = (object)null
                    });
                }

                _logger.LogInformation("CREATE_VNPAY_PAYMENT_URL_SUCCESS: URL tạo thành công - InvoiceId: {InvoiceId}", request.InvoiceId);

                return Ok(new
                {
                    success = true,
                    message = "Tạo URL thanh toán VNPay thành công",
                    data = new { paymentUrl = paymentUrl }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CREATE_VNPAY_PAYMENT_URL_ERROR: Lỗi khi tạo URL thanh toán VNPay - InvoiceId: {InvoiceId}", request?.InvoiceId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server",
                    error = ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpGet("vnpay/payment-callback")]
        public async Task<IActionResult> VnPayPaymentCallback()
        {
            try
            {
                _logger.LogInformation("VNPAY_PAYMENT_CALLBACK: Nhận callback từ VNPay");

                var isValid = await _invoicePaymentService.ProcessVnPayCallback(Request.Query);
                if (!isValid)
                {
                    _logger.LogWarning("VNPAY_PAYMENT_CALLBACK_FAILED: Callback xử lý thất bại");
                    return Redirect("http://localhost:3000/profile");
                }

                _logger.LogInformation("VNPAY_PAYMENT_CALLBACK_SUCCESS: Callback xử lý thành công");
                return Redirect("http://localhost:3000/profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPAY_PAYMENT_CALLBACK_ERROR: Lỗi khi xử lý callback VNPay");
                return Redirect("http://localhost:3000/profile");
            }
        }

        [HttpPost("momo/create-payment-url")]
        public async Task<IActionResult> CreateMomoPaymentUrl([FromBody] CreateMomoPaymentUrlRequest request)
        {
            try
            {
                _logger.LogInformation("CREATE_MOMO_PAYMENT_URL: Tạo URL thanh toán Momo - InvoiceId: {InvoiceId}", request.InvoiceId);

                if (request?.InvoiceId <= 0)
                {
                    _logger.LogWarning("CREATE_MOMO_PAYMENT_URL: InvoiceId không hợp lệ - InvoiceId: {InvoiceId}", request?.InvoiceId);
                    return BadRequest(new
                    {
                        success = false,
                        message = "InvoiceId không hợp lệ",
                        data = (object)null
                    });
                }

                var orderInfo = await _invoicePaymentService.GenerateMomoPaymentUrl(request.InvoiceId);
                if (orderInfo == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Không thể tạo URL thanh toán. Hóa đơn không tồn tại, đã thanh toán hoặc đã bị xóa",
                        data = (object)null
                    });
                }

                var momoResponse = await _momoService.CreatePaymentAsync(orderInfo);

                if (momoResponse == null || string.IsNullOrEmpty(momoResponse.PayUrl))
                {
                    _logger.LogError("CREATE_MOMO_PAYMENT_URL: Payment URL rỗng");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Không thể tạo URL thanh toán",
                        error = momoResponse?.Message,
                        data = (object)null
                    });
                }

                _logger.LogInformation("CREATE_MOMO_PAYMENT_URL_SUCCESS: URL tạo thành công - InvoiceId: {InvoiceId}", request.InvoiceId);

                return Ok(new
                {
                    success = true,
                    message = "Tạo URL thanh toán Momo thành công",
                    data = new { paymentUrl = momoResponse.PayUrl }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CREATE_MOMO_PAYMENT_URL_ERROR: Lỗi khi tạo URL thanh toán Momo - InvoiceId: {InvoiceId}", request?.InvoiceId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server",
                    error = ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpGet("momo/payment-callback")]
        public async Task<IActionResult> MomoPaymentCallback()
        {
            try
            {
                _logger.LogInformation("MOMO_PAYMENT_CALLBACK: Nhận callback từ Momo");

                var isValid = await _invoicePaymentService.ProcessMomoCallback(Request.Query);
                if (!isValid)
                {
                    _logger.LogWarning("MOMO_PAYMENT_CALLBACK_FAILED: Callback xử lý thất bại");
                    return Redirect("https://yourdomain.com/payment-failed");
                }

                _logger.LogInformation("MOMO_PAYMENT_CALLBACK_SUCCESS: Callback xử lý thành công");
                return Redirect("https://yourdomain.com/payment-success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MOMO_PAYMENT_CALLBACK_ERROR: Lỗi khi xử lý callback Momo");
                return Redirect("https://yourdomain.com/payment-error");
            }
        }

        [HttpGet("payment-info/{invoiceId}")]
        public async Task<IActionResult> GetPaymentInfo(int invoiceId)
        {
            try
            {
                _logger.LogInformation("GET_PAYMENT_INFO: Lấy thông tin thanh toán hóa đơn - InvoiceId: {InvoiceId}", invoiceId);

                var paymentInfo = await _invoicePaymentService.GetInvoicePaymentInfo(invoiceId);
                if (paymentInfo == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Hóa đơn không tồn tại",
                        data = (object)null
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin thanh toán thành công",
                    data = paymentInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET_PAYMENT_INFO_ERROR: Lỗi khi lấy thông tin thanh toán - InvoiceId: {InvoiceId}", invoiceId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server",
                    error = ex.Message,
                    data = (object)null
                });
            }
        }

        [HttpPost("update-payment-status")]
        public async Task<IActionResult> UpdatePaymentStatus([FromBody] UpdateInvoicePaymentRequest request)
        {
            try
            {
                _logger.LogInformation("UPDATE_PAYMENT_STATUS: Cập nhật trạng thái thanh toán - InvoiceId: {InvoiceId}, PaidAmount: {PaidAmount:C}",
                    request.InvoiceId, request.PaidAmount);

                var isUpdated = await _invoicePaymentService.UpdateInvoicePayment(
                    request.InvoiceId,
                    request.PaidAmount,
                    request.PaymentMethod);

                if (!isUpdated)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cập nhật trạng thái thanh toán thất bại",
                        data = (object)null
                    });
                }

                var paymentInfo = await _invoicePaymentService.GetInvoicePaymentInfo(request.InvoiceId);

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật trạng thái thanh toán thành công",
                    data = paymentInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UPDATE_PAYMENT_STATUS_ERROR: Lỗi khi cập nhật trạng thái thanh toán");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server",
                    error = ex.Message,
                    data = (object)null
                });
            }
        }
    }
}