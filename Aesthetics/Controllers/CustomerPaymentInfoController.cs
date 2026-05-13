using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Entities.Models.RequestModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Aesthetics.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerPaymentInfoController : ControllerBase
    {

        private readonly ILogger<CustomerPaymentInfoController> _logger;
        private readonly ICustomerPaymentInfoService _customerPaymentInfoService;

        public CustomerPaymentInfoController(
            ILogger<CustomerPaymentInfoController> logger,
            ICustomerPaymentInfoService customerPaymentInfoService)
        {
            _logger = logger;
            _customerPaymentInfoService = customerPaymentInfoService;
        }

        /// <summary>
        /// ✅ Tạo thông tin thanh toán mới cho khách hàng
        /// POST: api/customerpaymentinfo/create
        /// </summary>
        [HttpPost("createcustomerpayment")]
        public async Task<IActionResult> CreatePaymentInfo([FromBody] CreateCustomerPaymentModel request)
        {
            try
            {
                _logger.LogInformation("CREATE_PAYMENT_INFO_API_START: Tạo thông tin thanh toán - CustomerId: {CustomerId}, BankCode: {BankCode}",
                    request?.CustomerId, request?.BankCode);

               
                // ✅ Gọi service để tạo
                var result = await _customerPaymentInfoService.createcustomerpayment(request);

                if (result)
                {
                    _logger.LogInformation("CREATE_PAYMENT_INFO_API_SUCCESS: Tạo thông tin thanh toán thành công - CustomerId: {CustomerId}",
                        request.CustomerId);

                    return Ok(new
                    {
                        success = true,
                        message = "Tạo thông tin thanh toán thành công",
                        data = (object)null
                    });
                }
                else
                {
                    _logger.LogError("CREATE_PAYMENT_INFO_API_FAILED: Tạo thông tin thanh toán thất bại - CustomerId: {CustomerId}",
                        request.CustomerId);

                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Tạo thông tin thanh toán thất bại",
                        data = (object)null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CREATE_PAYMENT_INFO_API_EXCEPTION: Lỗi khi tạo thông tin thanh toán");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server khi tạo thông tin thanh toán",
                    error = ex.Message,
                    data = (object)null
                });
            }
        }

        /// <summary>
        /// ✅ Cập nhật thông tin thanh toán
        /// PUT: api/customerpaymentinfo/update
        /// </summary>
        [HttpPost("updatecustomerpayment")]
        public async Task<IActionResult> UpdatePaymentInfo([FromBody] updatecustomerpayment request)
        {
            try
            {
                _logger.LogInformation("UPDATE_PAYMENT_INFO_API_START: Cập nhật thông tin thanh toán - PaymentInfoId: {PaymentInfoId}, CustomerId: {CustomerId}",
                    request?.Id, request?.CustomerId);


                // ✅ Gọi service để cập nhật
                var result = await _customerPaymentInfoService.updatecustomerpayment(request);

                if (result)
                {
                    _logger.LogInformation("UPDATE_PAYMENT_INFO_API_SUCCESS: Cập nhật thông tin thanh toán thành công - PaymentInfoId: {PaymentInfoId}",
                        request.Id);

                    return Ok(new
                    {
                        success = true,
                        message = "Cập nhật thông tin thanh toán thành công",
                        data = (object)null
                    });
                }
                else
                {
                    _logger.LogError("UPDATE_PAYMENT_INFO_API_FAILED: Cập nhật thông tin thanh toán thất bại - PaymentInfoId: {PaymentInfoId}",
                        request.Id);

                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Cập nhật thông tin thanh toán thất bại",
                        data = (object)null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UPDATE_PAYMENT_INFO_API_EXCEPTION: Lỗi khi cập nhật thông tin thanh toán");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server khi cập nhật thông tin thanh toán",
                    error = ex.Message,
                    data = (object)null
                });
            }
        }

        /// <summary>
        /// ✅ Xóa thông tin thanh toán
        /// DELETE: api/customerpaymentinfo/delete
        /// </summary>
        [HttpPost("deletecustomerpayment")]
        public async Task<IActionResult> DeletePaymentInfo([FromBody] deletecustomerpayment request)
        {
            try
            {
                _logger.LogInformation("DELETE_PAYMENT_INFO_API_START: Xóa thông tin thanh toán - PaymentInfoId: {PaymentInfoId}",
                    request?.Id);

                // ✅ Gọi service để xóa
                var result = await _customerPaymentInfoService.deletecustomerpayment(request);

                if (result)
                {
                    _logger.LogInformation("DELETE_PAYMENT_INFO_API_SUCCESS: Xóa thông tin thanh toán thành công - PaymentInfoId: {PaymentInfoId}",
                        request.Id);

                    return Ok(new
                    {
                        success = true,
                        message = "Xóa thông tin thanh toán thành công",
                        data = (object)null
                    });
                }
                else
                {
                    _logger.LogError("DELETE_PAYMENT_INFO_API_FAILED: Xóa thông tin thanh toán thất bại - PaymentInfoId: {PaymentInfoId}",
                        request.Id);

                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Xóa thông tin thanh toán thất bại (có thể là tài khoản mặc định)",
                        data = (object)null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DELETE_PAYMENT_INFO_API_EXCEPTION: Lỗi khi xóa thông tin thanh toán");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server khi xóa thông tin thanh toán",
                    error = ex.Message,
                    data = (object)null
                });
            }
        }

        /// <summary>
        /// ✅ Lấy danh sách thông tin thanh toán của khách hàng
        /// POST: api/customerpaymentinfo/get-list
        /// </summary>
        [HttpPost("getlistcustomerpayment")]
        public async Task<IActionResult> GetPaymentInfoList([FromBody] getlistcustomerpayment request)
        {
            try
            {
                _logger.LogInformation("GET_LIST_PAYMENT_INFO_API_START: Lấy danh sách thông tin thanh toán - CustomerId: {CustomerId}",
                    request?.customerId);

                // ✅ Gọi service để lấy danh sách
                var result = await _customerPaymentInfoService.getlistcustomerpayment(request);

                _logger.LogInformation("GET_LIST_PAYMENT_INFO_API_SUCCESS: Lấy danh sách thành công - CustomerId: {CustomerId}, Count: {Count}",
                    request.customerId, result.TotalRecordCount);

				return Ok(result);
			}
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET_LIST_PAYMENT_INFO_API_EXCEPTION: Lỗi khi lấy danh sách thông tin thanh toán");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi server khi lấy danh sách thông tin thanh toán",
                    error = ex.Message,
                    data = (object)null
                });
            }
        }

    }
}