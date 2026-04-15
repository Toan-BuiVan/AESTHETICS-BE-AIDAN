using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.DTO.NetCore.DataObject.Model.Momo;
using Aesthetics.DTO.NetCore.DataObject.Model.VnPay;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
		private readonly IStaffRepository _staffRepository;
		private readonly ICustomerRepository _customerRepository;
		private readonly IConfiguration _configuration;

        public InvoicePaymentService(
            ILogger<InvoicePaymentService> logger,
            IInvoiceRepository invoiceRepository,
            IInvoiceDetailsRepository invoiceDetailsRepository,
			IStaffRepository staffRepository,
			ICustomerRepository customerRepository,
			IConfiguration configuration)
        {
            _logger = logger;
            _invoiceRepository = invoiceRepository;
            _invoiceDetailsRepository = invoiceDetailsRepository;
			_staffRepository = staffRepository;
			_customerRepository = customerRepository;
			_configuration = configuration;
		}

		/// <summary>
		/// Tạo Payment Model cho VNPay từ thông tin hóa đơn
		/// ✅ Nếu Status là ThanhToanMotPhan: thanh toán 30% của FinalPrice
		/// ✅ Nếu Status là ThanhToanToanBo: thanh toán 100% của FinalPrice
		/// ✅ Lần sau thanh toán sẽ là số tiền còn thiếu (OutstandingBalance)
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

				// ✅ Kiểm tra nếu hóa đơn đã thanh toán hoàn toàn
				decimal outstandingBalance = invoice.OutstandingBalance ?? 0;
				if (outstandingBalance <= 0)
				{
					_logger.LogWarning("GENERATE_VNPAY_URL_PAID: Hóa đơn đã thanh toán - InvoiceId: {InvoiceId}", invoiceId);
					return null;
				}

				// ✅ TÍNH SỐ TIỀN CẦN THANH TOÁN DỰA TRÊN STATUS
				decimal paymentAmount = CalculatePaymentAmountByStatus(invoice);

				_logger.LogInformation("GENERATE_VNPAY_URL_AMOUNT_CALCULATED: InvoiceId: {InvoiceId}, Status: {Status}, FinalPrice: {FinalPrice:C}, PaidAmount: {PaidAmount:C}, OutstandingBalance: {OutstandingBalance:C}, PaymentAmount: {PaymentAmount:C}",
					invoiceId, invoice.Status, invoice.FinalPrice, invoice.PaidAmount, outstandingBalance, paymentAmount);

				// ✅ KIỂM TRA KHÔNG VƯỢT QUÁ SỐ TIỀN CÒN NỢ
				if (paymentAmount > outstandingBalance)
				{
					_logger.LogWarning("GENERATE_VNPAY_URL_AMOUNT_EXCEEDED: Số tiền thanh toán vượt quá nợ còn lại - InvoiceId: {InvoiceId}, PaymentAmount: {PaymentAmount:C}, Outstanding: {OutstandingBalance:C}",
						invoiceId, paymentAmount, outstandingBalance);
					paymentAmount = outstandingBalance;
				}

				var paymentModel = new PaymentInformationModel
				{
					OrderID = invoiceId.ToString(),
					Name = invoice.Customer?.FullName ?? "KhachHang",
					OrderDescription = $"Thanh toan hoa don #{invoiceId}",
					Amount = (double)paymentAmount
				};

				_logger.LogInformation("GENERATE_VNPAY_URL_SUCCESS: Payment Model VNPay được tạo - InvoiceId: {InvoiceId}, Amount: {Amount:C}, Status: {Status}",
					invoiceId, paymentAmount, invoice.Status);

				return paymentModel;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GENERATE_VNPAY_URL_EXCEPTION: Lỗi khi tạo Payment Model VNPay - InvoiceId: {InvoiceId}", invoiceId);
				return null;
			}
		}

		/// <summary>
		/// ✅ HÀM PHỤ: Tính số tiền cần thanh toán dựa trên Status của hóa đơn
		/// - ThanhToanMotPhan: 30% của FinalPrice
		/// - ThanhToanToanBo: 100% của FinalPrice
		/// - Lần thanh toán sau: phần còn lại (OutstandingBalance)
		/// </summary>
		private decimal CalculatePaymentAmountByStatus(InvoiceEntity invoice)
		{
			try
			{
				decimal finalPrice = invoice.FinalPrice ?? (invoice.TotalMoney ?? 0);
				decimal paidAmount = invoice.PaidAmount ?? 0;
				decimal outstandingBalance = invoice.OutstandingBalance ?? 0;

				// ✅ Nếu đã thanh toán rồi, lần này thanh toán phần còn lại
				if (paidAmount > 0)
				{
					_logger.LogInformation("CALCULATE_PAYMENT_ALREADY_PAID: Hóa đơn đã có thanh toán trước đó - InvoiceId: {InvoiceId}, PaidAmount: {PaidAmount:C}, Outstanding: {OutstandingBalance:C}",
						invoice.Id, paidAmount, outstandingBalance);
					return outstandingBalance;
				}

				// ✅ TÍNH TOÁN LẦN THANH TOÁN ĐẦU TIÊN
				string status = invoice.Status ?? "ChuaThanhToan";

				if (status == "ThanhToanMotPhan")
				{
					// 30% của FinalPrice lần đầu
					decimal firstPaymentAmount = finalPrice * 0.30m;
					_logger.LogInformation("CALCULATE_PAYMENT_PARTIAL: Thanh toán một phần (30%) - InvoiceId: {InvoiceId}, FinalPrice: {FinalPrice:C}, FirstPaymentAmount: {FirstPaymentAmount:C}",
						invoice.Id, finalPrice, firstPaymentAmount);
					return firstPaymentAmount;
				}
				else if (status == "ThanhToanToanBo")
				{
					// 100% của FinalPrice
					_logger.LogInformation("CALCULATE_PAYMENT_FULL: Thanh toán toàn bộ (100%) - InvoiceId: {InvoiceId}, FinalPrice: {FinalPrice:C}",
						invoice.Id, finalPrice);
					return finalPrice;
				}
				else
				{
					// ChuaThanhToan: thanh toán 100% (hoặc có thể thanh toán 30% tuỳ logic)
					_logger.LogInformation("CALCULATE_PAYMENT_DEFAULT: Status mặc định - InvoiceId: {InvoiceId}, Status: {Status}, Amount: {Amount:C}",
						invoice.Id, status, finalPrice);
					return finalPrice;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CALCULATE_PAYMENT_EXCEPTION: Lỗi khi tính số tiền thanh toán - InvoiceId: {InvoiceId}", invoice?.Id);
				return invoice?.OutstandingBalance ?? 0;
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
				var vnpTransactionNo = collections["vnp_TransactionNo"].ToString();
				
				// ✅ LẤY THỜI GIAN THANH TOÁN TỪ VNPAY
				var vnpPayDate = collections["vnp_PayDate"].ToString(); 

				_logger.LogInformation("PROCESS_VNPAY_CALLBACK_DATA: TxnRef: {TxnRef}, Amount: {Amount}, ResponseCode: {ResponseCode}, OrderInfo: {OrderInfo}, TransactionNo: {TransactionNo}, PayDate: {PayDate}",
					vnpTxnRef, vnpAmount, vnpResponseCode, vnpOrderInfo, vnpTransactionNo, vnpPayDate);

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

                if (string.IsNullOrEmpty(vnpOrderInfo) || !vnpOrderInfo.Contains("OrderID:"))
                {
                    _logger.LogWarning("PROCESS_VNPAY_CALLBACK_INVALID_ORDER_INFO: OrderInfo không hợp lệ - OrderInfo: {OrderInfo}", vnpOrderInfo);
                    return false;
                }

                var orderIdPart = vnpOrderInfo.Split('|')[0]; 
                var invoiceIdStr = orderIdPart.Split(':')[1]; 

                if (!int.TryParse(invoiceIdStr, out int invoiceId))
                {
                    _logger.LogWarning("PROCESS_VNPAY_CALLBACK_INVALID_INVOICE_ID: InvoiceId không hợp lệ - InvoiceIdStr: {InvoiceIdStr}", invoiceIdStr);
                    return false;
                }

				DateTime? transactionDateTime = null;
				if (!string.IsNullOrEmpty(vnpPayDate) && vnpPayDate.Length == 14)
				{
					if (DateTime.TryParseExact(vnpPayDate, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
					{
						transactionDateTime = parsedDate;
						_logger.LogInformation("PROCESS_VNPAY_CALLBACK_PAY_DATE_PARSED: vnp_PayDate={PayDate} -> {DateTime}", vnpPayDate, parsedDate);
					}
					else
					{
						_logger.LogWarning("PROCESS_VNPAY_CALLBACK_PAY_DATE_PARSE_FAILED: Không thể parse vnp_PayDate={PayDate}", vnpPayDate);
					}
				}

				var isUpdated = await UpdateInvoicePaymentWithTransaction(invoiceId, actualAmount, "VNPay", vnpTxnRef, transactionDateTime);
				if (!isUpdated)
				{
					_logger.LogError("PROCESS_VNPAY_CALLBACK_UPDATE_FAILED: Cập nhật hóa đơn thất bại - InvoiceId: {InvoiceId}", invoiceId);
					return false;
				}

				_logger.LogInformation("PROCESS_VNPAY_CALLBACK_SUCCESS: Callback VNPay được xử lý thành công - InvoiceId: {InvoiceId}, Amount: {Amount:C}, TransactionNo: {TransactionNo}, PayDate: {PayDate}",
					invoiceId, actualAmount, vnpTransactionNo, transactionDateTime);

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
		/// ✅ Nếu Status là ThanhToanMotPhan: thanh toán 30% của FinalPrice
		/// ✅ Nếu Status là ThanhToanToanBo: thanh toán 100% của FinalPrice
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

				// ✅ TÍNH SỐ TIỀN CẦN THANH TOÁN DỰA TRÊN STATUS
				decimal paymentAmount = CalculatePaymentAmountByStatus(invoice);

				// ✅ KIỂM TRA KHÔNG VƯỢT QUÁ SỐ TIỀN CÒN NỢ
				if (paymentAmount > outstandingBalance)
				{
					_logger.LogWarning("GENERATE_MOMO_URL_AMOUNT_EXCEEDED: Số tiền thanh toán vượt quá nợ còn lại - InvoiceId: {InvoiceId}, PaymentAmount: {PaymentAmount:C}, Outstanding: {OutstandingBalance:C}",
						invoiceId, paymentAmount, outstandingBalance);
					paymentAmount = outstandingBalance;
				}

				// ✅ Build OrderInfo từ chi tiết hóa đơn
				string orderInfo = BuildOrderInfo(invoice);

				string uniqueOrderId = $"{invoiceId}_{DateTime.UtcNow.Ticks}";

				var momoModel = new OrderInfoModel
				{
					OrderId = uniqueOrderId,
					FullName = invoice.Customer?.FullName ?? "KhachHang",
					OrderInfo = orderInfo,
					Amount = paymentAmount.ToString("F0"),
				};

				_logger.LogInformation("GENERATE_MOMO_URL_SUCCESS: URL thanh toán Momo được tạo - InvoiceId: {InvoiceId}, UniqueOrderId: {UniqueOrderId}, OrderInfo: {OrderInfo}, Amount: {Amount:C}, Status: {Status}",
					invoiceId, uniqueOrderId, orderInfo, paymentAmount, invoice.Status);

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
				var transId = collections["transId"].ToString(); // ✅ Lấy mã giao dịch từ Momo

				_logger.LogInformation("PROCESS_MOMO_CALLBACK_DATA: OrderId: {OrderId}, Amount: {Amount}, ErrorCode: {ErrorCode}, TransId: {TransId}",
					orderId, amount, errorCode, transId);

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
				var isUpdated = await UpdateInvoicePaymentWithTransaction(invoiceId, parsedAmount, "Momo", transId);
				if (!isUpdated)
				{
					_logger.LogError("PROCESS_MOMO_CALLBACK_UPDATE_FAILED: Cập nhật hóa đơn thất bại - InvoiceId: {InvoiceId}", invoiceId);
					return false;
				}

				_logger.LogInformation("PROCESS_MOMO_CALLBACK_SUCCESS: Callback Momo được xử lý thành công - InvoiceId: {InvoiceId}, Amount: {Amount:C}, TransId: {TransId}",
					invoiceId, parsedAmount, transId);

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
		/// Update tiền thanh toán & trạng thái hóa đơn - THÊM PaymentDate
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

				// ✅ KIỂM TRA KHÔNG VƯỢT TỔNG TIỀN
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

				// ✅ CẬP NHẬT: Kiểm tra nếu hóa đơn có sản phẩm (ProductId != null)
				// Nếu có sản phẩm và thanh toán hết → OrderStatus = "DangXuLy"
				string orderStatus = invoice.OrderStatus;
				if (outstandingBalance <= 0)
				{
					bool hasProduct = await HasProductInInvoiceAsync(invoiceId);

					if (hasProduct)
					{
						orderStatus = "DangXuLy";
						_logger.LogInformation("UPDATE_INVOICE_PAYMENT_ORDER_STATUS: Hóa đơn có sản phẩm và thanh toán hết - InvoiceId: {InvoiceId}, OrderStatus cập nhật thành: {OrderStatus}",
							invoiceId, orderStatus);
					}
				}
				else
				{
					// ❌ Chưa thanh toán hết → giữ nguyên OrderStatus
					_logger.LogInformation("UPDATE_INVOICE_PAYMENT_PARTIAL: Thanh toán một phần - InvoiceId: {InvoiceId}, OutstandingBalance: {OutstandingBalance:C}, OrderStatus giữ nguyên: {OrderStatus}",
						invoiceId, outstandingBalance, orderStatus);
				}

				// ✅ UPDATE INVOICE ENTITY - THÊM PaymentDate
				invoice.PaidAmount = newPaidAmount;
				invoice.OutstandingBalance = outstandingBalance;
				invoice.Status = status;
				invoice.PaymentMethod = paymentMethod;
				invoice.OrderStatus = orderStatus;

				var updated = await _invoiceRepository.UpdateEntity(invoice);
				if (!updated)
				{
					_logger.LogError("UPDATE_INVOICE_PAYMENT_FAILED: Cập nhật hóa đơn thất bại - InvoiceId: {InvoiceId}", invoiceId);
					return false;
				}

				_logger.LogInformation("UPDATE_INVOICE_PAYMENT_SUCCESS: Cập nhật thanh toán thành công - InvoiceId: {InvoiceId}, OldPaid: {OldPaidAmount:C}, NewPaid: {NewPaidAmount:C}, Outstanding: {OutstandingBalance:C}, Status: {Status}, PaymentDate: {PaymentDate}",
					invoiceId, currentPaidAmount, newPaidAmount, outstandingBalance, status, invoice.PaymentDate);

				// ✅ UPDATE TẤT CẢ INVOICEDETAILS
				await UpdateInvoiceDetailsStatus(invoiceId, status);

				if (paidAmount > 0)
				{
					_logger.LogInformation("UPDATE_INVOICE_PAYMENT_ADD_POINTS_START: Cộng điểm thanh toán - InvoiceId: {InvoiceId}, PaidAmount: {PaidAmount:C}",
						invoiceId, paidAmount);

					// ✅ Cộng điểm mua hàng cho khách hàng
					if (invoice.CustomerId.HasValue)
					{
						await AddPurchasePointsToCustomer(invoice.CustomerId.Value, paidAmount, invoiceId);
					}

					// ✅ Cộng điểm bán hàng cho nhân viên
					if (invoice.StaffId.HasValue)
					{
						await AddSalesPointsToStaff(invoice.StaffId.Value, paidAmount, invoiceId);
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_INVOICE_PAYMENT_EXCEPTION: Lỗi khi cập nhật thanh toán hóa đơn - InvoiceId: {InvoiceId}", invoiceId);
				return false;
			}
		}


		/// <summary>
		/// ✅ Cộng điểm mua hàng cho khách hàng
		/// Công thức: 1 điểm = 10000 VND
		/// </summary>
		private async Task<bool> AddPurchasePointsToCustomer(int customerId, decimal invoiceAmount, int invoiceId)
		{
			try
			{
				_logger.LogInformation("ADD_PURCHASE_POINTS_START: Cộng điểm mua hàng - CustomerId: {CustomerId}, Amount: {Amount:C}, InvoiceId: {InvoiceId}",
					customerId, invoiceAmount, invoiceId);

				var customer = await _customerRepository.GetById(customerId);
				if (customer == null || customer.DeleteStatus)
				{
					_logger.LogWarning("ADD_PURCHASE_POINTS_CUSTOMER_NOT_FOUND: Khách hàng không tồn tại - CustomerId: {CustomerId}", customerId);
					return false;
				}

				// ✅ Tính điểm: 1 điểm = 1000 VND (có thể điều chỉnh hệ số)
				int pointsToAdd = (int)(invoiceAmount / 10000);

				if (pointsToAdd <= 0)
				{
					_logger.LogInformation("ADD_PURCHASE_POINTS_NO_POINTS: Số tiền không đủ để cộng điểm - Amount: {Amount:C}, Required: 1000", invoiceAmount);
					return true; // Không lỗi, chỉ không cộng
				}

				// ✅ Cộng điểm vào RatingPoints
				customer.RatingPoints = (customer.RatingPoints) + pointsToAdd;

				var updated = await _customerRepository.UpdateEntity(customer);
				if (!updated)
				{
					_logger.LogError("ADD_PURCHASE_POINTS_FAILED: Cộng điểm thất bại - CustomerId: {CustomerId}", customerId);
					return false;
				}

				_logger.LogInformation("ADD_PURCHASE_POINTS_SUCCESS: ✅ Cộng {Points} điểm mua hàng cho khách hàng - CustomerId: {CustomerId}, NewRatingPoints: {NewRatingPoints}, InvoiceId: {InvoiceId}",
					pointsToAdd, customerId, customer.RatingPoints, invoiceId);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ADD_PURCHASE_POINTS_EXCEPTION: Lỗi khi cộng điểm mua hàng - CustomerId: {CustomerId}", customerId);
				return false;
			}
		}

		/// <summary>
		/// ✅ Cộng điểm bán hàng cho nhân viên
		/// Công thức: 1 điểm = 20000 VND (có thể điều chỉnh)
		/// </summary>
		private async Task<bool> AddSalesPointsToStaff(int staffId, decimal invoiceAmount, int invoiceId)
		{
			try
			{
				_logger.LogInformation("ADD_SALES_POINTS_START: Cộng điểm bán hàng - StaffId: {StaffId}, Amount: {Amount:C}, InvoiceId: {InvoiceId}",
					staffId, invoiceAmount, invoiceId);

				var staff = await _staffRepository.GetById(staffId);
				if (staff == null || staff.DeleteStatus)
				{
					_logger.LogWarning("ADD_SALES_POINTS_STAFF_NOT_FOUND: Nhân viên không tồn tại - StaffId: {StaffId}", staffId);
					return false;
				}

				// ✅ Tính điểm: 1 điểm = 20000 VND (có thể điều chỉnh hệ số)
				int pointsToAdd = (int)(invoiceAmount / 20000);

				if (pointsToAdd <= 0)
				{
					_logger.LogInformation("ADD_SALES_POINTS_NO_POINTS: Số tiền không đủ để cộng điểm - Amount: {Amount:C}, Required: 5000", invoiceAmount);
					return true; // Không lỗi, chỉ không cộng
				}

				// ✅ Cộng điểm vào SalesPoints
				staff.SalesPoints = (staff.SalesPoints ?? 0) + pointsToAdd;

				var updated = await _staffRepository.UpdateEntity(staff);
				if (!updated)
				{
					_logger.LogError("ADD_SALES_POINTS_FAILED: Cộng điểm thất bại - StaffId: {StaffId}", staffId);
					return false;
				}

				_logger.LogInformation("ADD_SALES_POINTS_SUCCESS: ✅ Cộng {Points} điểm bán hàng cho nhân viên - StaffId: {StaffId}, NewSalesPoints: {NewSalesPoints}, InvoiceId: {InvoiceId}",
					pointsToAdd, staffId, staff.SalesPoints, invoiceId);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ADD_SALES_POINTS_EXCEPTION: Lỗi khi cộng điểm bán hàng - StaffId: {StaffId}", staffId);
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
		/// ✅ HÀM PHỤ: Kiểm tra xem hóa đơn có sản phẩm (ProductId != null) không
		/// </summary>
		private async Task<bool> HasProductInInvoiceAsync(int invoiceId)
		{
			try
			{
				_logger.LogInformation("HAS_PRODUCT_IN_INVOICE_START: Kiểm tra ProductId - InvoiceId: {InvoiceId}", invoiceId);

				// ✅ CHỈ lấy InvoiceDetails có ProductId
				var details = await _invoiceDetailsRepository.FindByPredicate(x =>
					x.InvoiceId == invoiceId &&
					x.ProductId.HasValue &&
					x.ProductId > 0 &&
					!x.DeleteStatus);

				bool hasProduct = details.Any();

				_logger.LogInformation("HAS_PRODUCT_IN_INVOICE_RESULT: InvoiceId: {InvoiceId}, HasProduct: {HasProduct}, ProductCount: {Count}",
					invoiceId, hasProduct, details.Count());

				return hasProduct;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "HAS_PRODUCT_IN_INVOICE_EXCEPTION: Lỗi khi kiểm tra sản phẩm - InvoiceId: {InvoiceId}",
					invoiceId);
				return false;
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
		/// <summary>

		/// <summary>
		/// ✅ HÀM MỚI: Update tiền + TransactionId từ callback payment
		/// ⭐ Gọi UpdateInvoicePayment để xử lý logic tiền tệ, rồi update TransactionId
		/// </summary>
		/// 
		/// <summary>
		/// ✅ Xử lý thanh toán thành công với TransactionId - Update tiền, status & VNPayTransactionDate
		/// </summary>
		private async Task<bool> UpdateInvoicePaymentWithTransaction(int invoiceId, decimal paidAmount, string paymentMethod, string transactionId, DateTime? transactionDateTime = null)
		{
			try
			{
				_logger.LogInformation("UPDATE_INVOICE_PAYMENT_WITH_TRANSACTION_START: InvoiceId={InvoiceId}, Amount={Amount:C}, Method={Method}, TransactionId={TransactionId}, TransactionDateTime={TransactionDateTime}",
					invoiceId, paidAmount, paymentMethod, transactionId, transactionDateTime);

				var invoice = await _invoiceRepository.GetById(invoiceId);
				if (invoice == null || invoice.DeleteStatus)
				{
					_logger.LogWarning("UPDATE_INVOICE_PAYMENT_WITH_TRANSACTION_NOT_FOUND: InvoiceId={InvoiceId}", invoiceId);
					return false;
				}

				// ✅ TÍNH TOÁN PAIDAMOUNT MỚI
				decimal currentPaidAmount = invoice.PaidAmount ?? 0;
				decimal newPaidAmount = currentPaidAmount + paidAmount;
				decimal finalPrice = invoice.FinalPrice ?? (invoice.TotalMoney ?? 0);

				if (newPaidAmount > finalPrice)
				{
					_logger.LogWarning("UPDATE_INVOICE_PAYMENT_WITH_TRANSACTION_EXCEEDED: NewPaid={NewPaid:C} > FinalPrice={FinalPrice:C}", newPaidAmount, finalPrice);
					newPaidAmount = finalPrice;
				}

				decimal outstandingBalance = finalPrice - newPaidAmount;
				string status = GetInvoicePaymentStatus(newPaidAmount, finalPrice);

				// ✅ Update OrderStatus nếu thanh toán hết
				string orderStatus = invoice.OrderStatus;
				if (outstandingBalance <= 0)
				{
					bool hasProduct = await HasProductInInvoiceAsync(invoiceId);
					if (hasProduct)
					{
						orderStatus = "DangXuLy";
						_logger.LogInformation("UPDATE_INVOICE_PAYMENT_WITH_TRANSACTION_ORDER_STATUS: OrderStatus cập nhật thành DangXuLy");
					}
				}

				// ✅ UPDATE INVOICE ENTITY
				invoice.PaidAmount = newPaidAmount;
				invoice.OutstandingBalance = outstandingBalance;
				invoice.Status = status;
				invoice.PaymentMethod = paymentMethod;
				invoice.OrderStatus = orderStatus;
				invoice.TransactionId = transactionId;

				
				if (transactionDateTime.HasValue)
				{
					invoice.PaymentDate = transactionDateTime.Value;
					_logger.LogInformation("UPDATE_INVOICE_PAYMENT_WITH_TRANSACTION_SET_PAYMENT_DATE: SET từ vnp_PayDate (CHÍNH XÁC)={PaymentDate}", transactionDateTime.Value);
				}
				else
				{
					invoice.PaymentDate = DateTime.Now;
					_logger.LogWarning("UPDATE_INVOICE_PAYMENT_WITH_TRANSACTION_SET_PAYMENT_DATE_FALLBACK: Không có vnp_PayDate, dùng DateTime.Now (có thể KHÔNG CHÍNH XÁC)={PaymentDate}", DateTime.Now);
				}

				var updated = await _invoiceRepository.UpdateEntity(invoice);
				if (!updated)
				{
					_logger.LogError("UPDATE_INVOICE_PAYMENT_WITH_TRANSACTION_FAILED: Cập nhật thất bại");
					return false;
				}

				_logger.LogInformation("UPDATE_INVOICE_PAYMENT_WITH_TRANSACTION_SUCCESS: InvoiceId={InvoiceId}, NewPaid={NewPaid:C}, Outstanding={Outstanding:C}, Status={Status}, PaymentDate={PaymentDate}",
					invoiceId, newPaidAmount, outstandingBalance, status, invoice.PaymentDate);

				// ✅ Update InvoiceDetails & Add Points
				await UpdateInvoiceDetailsStatus(invoiceId, status);
				if (paidAmount > 0)
				{
					if (invoice.CustomerId.HasValue)
						await AddPurchasePointsToCustomer(invoice.CustomerId.Value, paidAmount, invoiceId);
					if (invoice.StaffId.HasValue)
						await AddSalesPointsToStaff(invoice.StaffId.Value, paidAmount, invoiceId);
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_INVOICE_PAYMENT_WITH_TRANSACTION_EXCEPTION: InvoiceId={InvoiceId}", invoiceId);
				return false;
			}
		}
	}
}