using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.DTO.NetCore.DataObject.Model.Momo;
using Aesthetics.DTO.NetCore.DataObject.Model.VnPay;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
    public class InvoicePaymentService : IInvoicePaymentService
    {
        private readonly ILogger<InvoicePaymentService> _logger;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IInvoiceDetailsRepository _invoiceDetailsRepository;

        public InvoicePaymentService(
            ILogger<InvoicePaymentService> logger,
            IInvoiceRepository invoiceRepository,
            IInvoiceDetailsRepository invoiceDetailsRepository)
        {
            _logger = logger;
            _invoiceRepository = invoiceRepository;
            _invoiceDetailsRepository = invoiceDetailsRepository;
        }

        /// <summary>
        /// Tạo Payment Model cho VNPay từ thông tin hóa đơn
        /// </summary>
        public async Task<PaymentInformationModel> GenerateVnPayPaymentUrl(int invoiceId, HttpContext context)
        {
            try
            {
                _logger.LogInformation("GENERATE_VNPAY_URL_START: Tạo Payment Model VNPay - InvoiceId: {InvoiceId}", invoiceId);

                var invoice = await _invoiceRepository.GetById(invoiceId);
                if (invoice == null || invoice.DeleteStatus)
                {
                    _logger.LogWarning("GENERATE_VNPAY_URL_INVOICE_NOT_FOUND: Hóa đơn không tồn tại - InvoiceId: {InvoiceId}", invoiceId);
                    return null;
                }

                decimal outstandingBalance = invoice.OutstandingBalance ?? 0;
                if (outstandingBalance <= 0)
                {
                    _logger.LogWarning("GENERATE_VNPAY_URL_PAID: Hóa đơn đã thanh toán - InvoiceId: {InvoiceId}", invoiceId);
                    return null;
                }

                var paymentModel = new PaymentInformationModel
                {
                    OrderID = invoiceId.ToString(),
                    Name = invoice.Customer?.FullName ?? "KhachHang",
                    OrderDescription = $"Thanh toan hoa don #{invoiceId}",
                    Amount = (double)outstandingBalance
                };

                _logger.LogInformation("GENERATE_VNPAY_URL_SUCCESS: Payment Model VNPay được tạo - InvoiceId: {InvoiceId}, Amount: {Amount:C}",
                    invoiceId, outstandingBalance);

                return paymentModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GENERATE_VNPAY_URL_EXCEPTION: Lỗi khi tạo Payment Model VNPay - InvoiceId: {InvoiceId}", invoiceId);
                return null;
            }
        }

        /// <summary>
        /// Xử lý callback khi VNPay thanh toán thành công
        /// </summary>
        public async Task<bool> ProcessVnPayCallback(IQueryCollection collections)
        {
            try
            {
                _logger.LogInformation("PROCESS_VNPAY_CALLBACK_START: Xử lý callback từ VNPay");

                var vnpTxnRef = collections["vnp_TxnRef"].ToString();
                var vnpAmount = collections["vnp_Amount"].ToString();
                var vnpResponseCode = collections["vnp_ResponseCode"].ToString();
                var vnpOrderInfo = collections["vnp_OrderInfo"].ToString();

                _logger.LogInformation("PROCESS_VNPAY_CALLBACK_DATA: TxnRef: {TxnRef}, Amount: {Amount}, ResponseCode: {ResponseCode}, OrderInfo: {OrderInfo}",
                    vnpTxnRef, vnpAmount, vnpResponseCode, vnpOrderInfo);

                if (vnpResponseCode != "00")
                {
                    _logger.LogWarning("PROCESS_VNPAY_CALLBACK_FAILED: Thanh toán VNPay thất bại - ResponseCode: {ResponseCode}", vnpResponseCode);
                    return false;
                }

                if (!decimal.TryParse(vnpAmount, out decimal amountVnp))
                {
                    _logger.LogWarning("PROCESS_VNPAY_CALLBACK_INVALID_AMOUNT: Số tiền không hợp lệ - Amount: {Amount}", vnpAmount);
                    return false;
                }

                decimal actualAmount = amountVnp / 100;

                // Parse OrderInfo để lấy InvoiceId
                // Format: OrderID:92|KhachHang|Thanh toan hoa don #92|2000000
                if (string.IsNullOrEmpty(vnpOrderInfo) || !vnpOrderInfo.Contains("OrderID:"))
                {
                    _logger.LogWarning("PROCESS_VNPAY_CALLBACK_INVALID_ORDER_INFO: OrderInfo không hợp lệ - OrderInfo: {OrderInfo}", vnpOrderInfo);
                    return false;
                }

                var orderIdPart = vnpOrderInfo.Split('|')[0]; // "OrderID:92"
                var invoiceIdStr = orderIdPart.Split(':')[1]; // "92"

                if (!int.TryParse(invoiceIdStr, out int invoiceId))
                {
                    _logger.LogWarning("PROCESS_VNPAY_CALLBACK_INVALID_INVOICE_ID: InvoiceId không hợp lệ - InvoiceIdStr: {InvoiceIdStr}", invoiceIdStr);
                    return false;
                }

                // Cập nhật thanh toán hóa đơn
                var isUpdated = await UpdateInvoicePayment(invoiceId, actualAmount, "VNPay");
                if (!isUpdated)
                {
                    _logger.LogError("PROCESS_VNPAY_CALLBACK_UPDATE_FAILED: Cập nhật hóa đơn thất bại - InvoiceId: {InvoiceId}", invoiceId);
                    return false;
                }

                _logger.LogInformation("PROCESS_VNPAY_CALLBACK_SUCCESS: Callback VNPay được xử lý thành công - InvoiceId: {InvoiceId}, Amount: {Amount:C}",
                    invoiceId, actualAmount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PROCESS_VNPAY_CALLBACK_EXCEPTION: Lỗi khi xử lý callback VNPay");
                return false;
            }
        }

        /// <summary>
        /// Tạo URL thanh toán Momo cho hóa đơn
        /// </summary>
        public async Task<OrderInfoModel> GenerateMomoPaymentUrl(int invoiceId)
        {
            try
            {
                _logger.LogInformation("GENERATE_MOMO_URL_START: Tạo URL thanh toán Momo - InvoiceId: {InvoiceId}", invoiceId);

                var invoice = await _invoiceRepository.GetById(invoiceId);
                if (invoice == null || invoice.DeleteStatus)
                {
                    _logger.LogWarning("GENERATE_MOMO_URL_INVOICE_NOT_FOUND: Hóa đơn không tồn tại - InvoiceId: {InvoiceId}", invoiceId);
                    return null;
                }

                decimal outstandingBalance = invoice.OutstandingBalance ?? 0;
                if (outstandingBalance <= 0)
                {
                    _logger.LogWarning("GENERATE_MOMO_URL_PAID: Hóa đơn đã thanh toán - InvoiceId: {InvoiceId}", invoiceId);
                    return null;
                }

                // ✅ Build OrderInfo từ chi tiết hóa đơn
                string orderInfo = BuildOrderInfo(invoice);

                string uniqueOrderId = $"{invoiceId}_{DateTime.UtcNow.Ticks}";

                var momoModel = new OrderInfoModel
                {
                    OrderId = uniqueOrderId,
                    FullName = invoice.Customer?.FullName ?? "KhachHang",
                    OrderInfo = orderInfo,  // ✅ Tên dịch vụ + buổi điều trị
                    Amount = outstandingBalance.ToString("F0"),
                };

                _logger.LogInformation("GENERATE_MOMO_URL_SUCCESS: URL thanh toán Momo được tạo - InvoiceId: {InvoiceId}, UniqueOrderId: {UniqueOrderId}, OrderInfo: {OrderInfo}, Amount: {Amount:C}",
                    invoiceId, uniqueOrderId, orderInfo, outstandingBalance);

                return momoModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GENERATE_MOMO_URL_EXCEPTION: Lỗi khi tạo URL thanh toán Momo - InvoiceId: {InvoiceId}", invoiceId);
                return null;
            }
        }

        /// <summary>
        /// ✅ Xây dựng OrderInfo từ chi tiết hóa đơn (Thanh toán + Tên dịch vụ + Tên buổi điều trị)
        /// </summary>
        private string BuildOrderInfo(InvoiceEntity invoice)
        {
            try
            {
                var orderInfoParts = new List<string>();

                if (invoice.InvoiceDetails == null || invoice.InvoiceDetails.Count == 0)
                {
                    _logger.LogWarning("BUILD_ORDER_INFO_NO_DETAILS: Hóa đơn không có chi tiết - InvoiceId: {InvoiceId}", invoice.Id);
                    return $"Thanh toan hoa don #{invoice.Id}";
                }

                // ✅ Lấy danh sách tên dịch vụ và buổi điều trị với tiền tố "Thanh toán"
                foreach (var detail in invoice.InvoiceDetails)
                {
                    var serviceName = detail.Service?.ServiceName ?? "Dich vu";
                    var sessionName = detail.TreatmentSession?.SessionName;

                    string itemInfo;
                    if (!string.IsNullOrEmpty(sessionName))
                    {
                        itemInfo = $"Thanh toan {serviceName} - {sessionName}";
                    }
                    else
                    {
                        itemInfo = $"Thanh toan {serviceName}";
                    }

                    orderInfoParts.Add(itemInfo);
                }

                // ✅ Nếu có quá 3 item thì chỉ lấy 3 item đầu tiên + thêm "..."
                if (orderInfoParts.Count > 3)
                {
                    var shortList = string.Join("; ", orderInfoParts.Take(3));
                    return $"{shortList}; ...";
                }

                var finalOrderInfo = string.Join("; ", orderInfoParts);

                _logger.LogInformation("BUILD_ORDER_INFO_SUCCESS: OrderInfo được xây dựng - InvoiceId: {InvoiceId}, OrderInfo: {OrderInfo}",
                    invoice.Id, finalOrderInfo);

                return finalOrderInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BUILD_ORDER_INFO_EXCEPTION: Lỗi khi xây dựng OrderInfo - InvoiceId: {InvoiceId}", invoice.Id);
                return $"Thanh toan hoa don #{invoice.Id}";
            }
        }

        /// <summary>
        /// Xử lý callback khi Momo thanh toán thành công
        /// </summary>
        public async Task<bool> ProcessMomoCallback(IQueryCollection collections)
        {
            try
            {
                _logger.LogInformation("PROCESS_MOMO_CALLBACK_START: Xử lý callback từ Momo");

                var orderId = collections["orderId"].ToString();
                var amount = collections["amount"].ToString();
                var errorCode = collections["errorCode"].ToString();

                _logger.LogInformation("PROCESS_MOMO_CALLBACK_DATA: OrderId: {OrderId}, Amount: {Amount}, ErrorCode: {ErrorCode}",
                    orderId, amount, errorCode);

                // ✅ Parse OrderId để lấy InvoiceId (format: "92_637893284957812345")
                if (string.IsNullOrEmpty(orderId) || !orderId.Contains("_"))
                {
                    _logger.LogWarning("PROCESS_MOMO_CALLBACK_INVALID_ORDER_ID: OrderId không hợp lệ - OrderId: {OrderId}", orderId);
                    return false;
                }

                var orderIdParts = orderId.Split('_');
                if (!int.TryParse(orderIdParts[0], out int invoiceId))
                {
                    _logger.LogWarning("PROCESS_MOMO_CALLBACK_INVALID_INVOICE_ID: InvoiceId không hợp lệ - InvoiceIdPart: {InvoiceIdPart}", orderIdParts[0]);
                    return false;
                }

                if (errorCode != "0")
                {
                    _logger.LogWarning("PROCESS_MOMO_CALLBACK_FAILED: Thanh toán Momo thất bại - ErrorCode: {ErrorCode}", errorCode);
                    return false;
                }

                if (!decimal.TryParse(amount, out decimal parsedAmount))
                {
                    _logger.LogWarning("PROCESS_MOMO_CALLBACK_INVALID_AMOUNT: Số tiền không hợp lệ - Amount: {Amount}", amount);
                    return false;
                }

                // Cập nhật thanh toán hóa đơn
                var isUpdated = await UpdateInvoicePayment(invoiceId, parsedAmount, "Momo");
                if (!isUpdated)
                {
                    _logger.LogError("PROCESS_MOMO_CALLBACK_UPDATE_FAILED: Cập nhật hóa đơn thất bại - InvoiceId: {InvoiceId}", invoiceId);
                    return false;
                }

                _logger.LogInformation("PROCESS_MOMO_CALLBACK_SUCCESS: Callback Momo được xử lý thành công - InvoiceId: {InvoiceId}, Amount: {Amount:C}",
                    invoiceId, parsedAmount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PROCESS_MOMO_CALLBACK_EXCEPTION: Lỗi khi xử lý callback Momo");
                return false;
            }
        }

        /// <summary>
        /// ✅ HÀM CHÍNH: Xử lý thanh toán thành công - Update tiền & status
        /// Được gọi sau khi thanh toán hoàn tất (VNPay, Momo, hoặc Direct)
        /// </summary>
        /// <param name="invoiceId">ID hóa đơn</param>
        /// <param name="paidAmount">Số tiền thanh toán lần này</param>
        /// <param name="paymentMethod">Phương thức thanh toán (VNPay, Momo, TienMat)</param>
        /// <returns>InvoicePaymentInfoModel với dữ liệu mới nhất</returns>
        public async Task<InvoicePaymentInfoModel> HandleSuccessfulPayment(int invoiceId, decimal paidAmount, string paymentMethod)
        {
            try
            {
                _logger.LogInformation("HANDLE_SUCCESSFUL_PAYMENT_START: Xử lý thanh toán thành công - InvoiceId: {InvoiceId}, Amount: {Amount:C}, Method: {Method}",
                    invoiceId, paidAmount, paymentMethod);

                // 🔴 BƯỚC 1: Update tiền & status
                var isUpdated = await UpdateInvoicePayment(invoiceId, paidAmount, paymentMethod);
                if (!isUpdated)
                {
                    _logger.LogError("HANDLE_SUCCESSFUL_PAYMENT_UPDATE_FAILED: Cập nhật hóa đơn thất bại - InvoiceId: {InvoiceId}", invoiceId);
                    return null;
                }

                _logger.LogInformation("HANDLE_SUCCESSFUL_PAYMENT_UPDATE_SUCCESS: Cập nhật hóa đơn thành công - InvoiceId: {InvoiceId}", invoiceId);

                // 🟡 BƯỚC 2: Lấy thông tin hóa đơn mới nhất sau update
                var updatedPaymentInfo = await GetInvoicePaymentInfo(invoiceId);
                if (updatedPaymentInfo == null)
                {
                    _logger.LogWarning("HANDLE_SUCCESSFUL_PAYMENT_GET_INFO_FAILED: Không thể lấy thông tin hóa đơn mới - InvoiceId: {InvoiceId}", invoiceId);
                    return null;
                }

                _logger.LogInformation("HANDLE_SUCCESSFUL_PAYMENT_SUCCESS: Xử lý thanh toán thành công hoàn toàn - InvoiceId: {InvoiceId}, NewPaidAmount: {PaidAmount:C}, NewOutstanding: {Outstanding:C}, Status: {Status}",
                    invoiceId, updatedPaymentInfo.PaidAmount, updatedPaymentInfo.OutstandingBalance, updatedPaymentInfo.Status);

                // 🟢 BƯỚC 3: Return dữ liệu mới nhất cho frontend
                return updatedPaymentInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HANDLE_SUCCESSFUL_PAYMENT_EXCEPTION: Lỗi khi xử lý thanh toán thành công - InvoiceId: {InvoiceId}", invoiceId);
                return null;
            }
        }

        /// <summary>
        /// Update tiền thanh toán & trạng thái hóa đơn
        /// (FUNCTION CỦA BẠN - ĐÃ CÓ)
        /// </summary>
        public async Task<bool> UpdateInvoicePayment(int invoiceId, decimal paidAmount, string paymentMethod)
        {
            try
            {
                _logger.LogInformation("UPDATE_INVOICE_PAYMENT_START: Cập nhật thanh toán hóa đơn - InvoiceId: {InvoiceId}, PaidAmount: {PaidAmount:C}, PaymentMethod: {PaymentMethod}",
                    invoiceId, paidAmount, paymentMethod);

                var invoice = await _invoiceRepository.GetById(invoiceId);
                if (invoice == null || invoice.DeleteStatus)
                {
                    _logger.LogWarning("UPDATE_INVOICE_PAYMENT_NOT_FOUND: Hóa đơn không tồn tại - InvoiceId: {InvoiceId}", invoiceId);
                    return false;
                }

                // ✅ TÍNH TOÁN PAIDAMOUNT MỚI (CỘNG DỒN)
                decimal currentPaidAmount = invoice.PaidAmount ?? 0;
                decimal newPaidAmount = currentPaidAmount + paidAmount;
                decimal finalPrice = invoice.FinalPrice ?? (invoice.TotalMoney ?? 0);

                // ✅ KIỂM TRA KHÔNG VƯỢT QUÁ TỔNG TIỀN
                if (newPaidAmount > finalPrice)
                {
                    _logger.LogWarning("UPDATE_INVOICE_PAYMENT_EXCEEDED: Số tiền thanh toán vượt quá tổng tiền hóa đơn - InvoiceId: {InvoiceId}, NewPaidAmount: {NewPaidAmount:C}, FinalPrice: {FinalPrice:C}",
                        invoiceId, newPaidAmount, finalPrice);
                    newPaidAmount = finalPrice;
                }

                // ✅ TÍNH OUTSTANDINGBALANCE (SỐ TIỀN CÒN NỢ)
                decimal outstandingBalance = finalPrice - newPaidAmount;

                // ✅ XÁC ĐỊNH STATUS
                string status = GetInvoicePaymentStatus(newPaidAmount, finalPrice);

                // ✅ UPDATE INVOICE ENTITY
                invoice.PaidAmount = newPaidAmount;
                invoice.OutstandingBalance = outstandingBalance;
                invoice.Status = status;
                invoice.PaymentMethod = paymentMethod;

                var updated = await _invoiceRepository.UpdateEntity(invoice);
                if (!updated)
                {
                    _logger.LogError("UPDATE_INVOICE_PAYMENT_FAILED: Cập nhật hóa đơn thất bại - InvoiceId: {InvoiceId}", invoiceId);
                    return false;
                }

                _logger.LogInformation("UPDATE_INVOICE_PAYMENT_SUCCESS: Cập nhật thanh toán thành công - InvoiceId: {InvoiceId}, OldPaid: {OldPaidAmount:C}, NewPaid: {NewPaidAmount:C}, Outstanding: {OutstandingBalance:C}, Status: {Status}",
                    invoiceId, currentPaidAmount, newPaidAmount, outstandingBalance, status);

                // ✅ UPDATE TẤT CẢ INVOICEDETAILS
                await UpdateInvoiceDetailsStatus(invoiceId, status);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UPDATE_INVOICE_PAYMENT_EXCEPTION: Lỗi khi cập nhật thanh toán hóa đơn - InvoiceId: {InvoiceId}", invoiceId);
                return false;
            }
        }

        /// <summary>
        /// Lấy thông tin thanh toán mới nhất của hóa đơn
        /// </summary>
        public async Task<InvoicePaymentInfoModel> GetInvoicePaymentInfo(int invoiceId)
        {
            try
            {
                _logger.LogInformation("GET_INVOICE_PAYMENT_INFO_START: Lấy thông tin thanh toán hóa đơn - InvoiceId: {InvoiceId}", invoiceId);

                var invoice = await _invoiceRepository.GetById(invoiceId);
                if (invoice == null || invoice.DeleteStatus)
                {
                    _logger.LogWarning("GET_INVOICE_PAYMENT_INFO_NOT_FOUND: Hóa đơn không tồn tại - InvoiceId: {InvoiceId}", invoiceId);
                    return null;
                }

                decimal totalAmount = invoice.FinalPrice ?? invoice.TotalMoney ?? 0;
                decimal paidAmount = invoice.PaidAmount ?? 0;
                decimal outstandingBalance = invoice.OutstandingBalance ?? 0;

                var paymentInfo = new InvoicePaymentInfoModel
                {
                    InvoiceId = invoiceId,
                    CustomerId = invoice.CustomerId ?? 0,
                    CustomerName = invoice.Customer?.FullName ?? "N/A",
                    TotalAmount = totalAmount,
                    PaidAmount = paidAmount,
                    OutstandingBalance = outstandingBalance,
                    Status = invoice.Status,
                    PaymentMethod = invoice.PaymentMethod,
                    DateCreated = invoice.DateCreated,
                    OrderStatus = invoice.OrderStatus
                };

                _logger.LogInformation("GET_INVOICE_PAYMENT_INFO_SUCCESS: Lấy thông tin thanh toán thành công - InvoiceId: {InvoiceId}, Total: {Total:C}, Paid: {Paid:C}, Outstanding: {Outstanding:C}, Status: {Status}",
                    invoiceId, totalAmount, paidAmount, outstandingBalance, invoice.Status);

                return paymentInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET_INVOICE_PAYMENT_INFO_EXCEPTION: Lỗi khi lấy thông tin thanh toán hóa đơn - InvoiceId: {InvoiceId}", invoiceId);
                return null;
            }
        }

        /// <summary>
        /// Cập nhật status cho tất cả InvoiceDetails
        /// </summary>
        private async Task UpdateInvoiceDetailsStatus(int invoiceId, string status)
        {
            try
            {
                var details = await _invoiceDetailsRepository.FindByPredicate(x =>
                    x.InvoiceId == invoiceId && !x.DeleteStatus);

                if (!details.Any())
                {
                    _logger.LogInformation("UPDATE_INVOICE_DETAILS_STATUS_NO_DETAILS: Không có chi tiết hóa đơn - InvoiceId: {InvoiceId}", invoiceId);
                    return;
                }

                int successCount = 0;
                foreach (var detail in details)
                {
                    detail.Status = status;
                    var updated = await _invoiceDetailsRepository.UpdateEntity(detail);
                    if (updated)
                    {
                        successCount++;
                    }
                }

                _logger.LogInformation("UPDATE_INVOICE_DETAILS_STATUS_SUCCESS: Cập nhật status chi tiết hóa đơn - InvoiceId: {InvoiceId}, TotalDetails: {TotalDetails}, UpdatedDetails: {UpdatedDetails}, Status: {Status}",
                    invoiceId, details.Count(), successCount, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UPDATE_INVOICE_DETAILS_STATUS_EXCEPTION: Lỗi khi cập nhật status chi tiết hóa đơn - InvoiceId: {InvoiceId}", invoiceId);
            }
        }

        /// <summary>
        /// Xác định trạng thái thanh toán dựa trên số tiền thanh toán
        /// </summary>
        private string GetInvoicePaymentStatus(decimal paidAmount, decimal finalPrice)
        {
            if (paidAmount <= 0)
                return "ChuaThanhToan";      // ❌ Chưa thanh toán

            if (paidAmount >= finalPrice)
                return "DaThanhToan";        // ✅ Đã thanh toán đủ

            return "ThanhToanMotPhan";       // 💰 Thanh toán một phần
        }
    }
}