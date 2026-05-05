using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.ICommonService;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
	public class RefundService : IRefundServcie
	{

		private readonly ILogger<RefundService> _logger;
		private readonly IRefundRepository _refundRepository;
		private readonly IInvoiceRepository _invoiceRepository;
		private readonly ICustomerRepository _customerRepository;
		private readonly IStaffRepository _staffRepository;
		private readonly ICustomerPaymentInfoRepository _customerPaymentInfoRepository;
		private readonly IConfiguration _configuration;
		private readonly ICommonService _commonService;


		public RefundService(
			ILogger<RefundService> logger,
			IRefundRepository refundRepository,
			IInvoiceRepository invoiceRepository,
			ICustomerRepository customerRepository,
			IStaffRepository staffRepository,
			ICustomerPaymentInfoRepository customerPaymentInfoRepository,
			IConfiguration configuration,
			ICommonService commonService)
		{
			_logger = logger;
			_refundRepository = refundRepository;
			_invoiceRepository = invoiceRepository;
			_customerRepository = customerRepository;
			_staffRepository = staffRepository;
			_customerPaymentInfoRepository = customerPaymentInfoRepository;
			_configuration = configuration;
			_commonService = commonService;
		}



		/// <summary>
		/// Tạo yêu cầu hoàn tiền mới
		/// </summary>
		public async Task<bool> createrefundservice(CreateRefundModel model)
		{
			try
			{
				_logger.LogInformation("CREATE_REFUND_START: Tạo yêu cầu hoàn tiền - InvoiceId: {InvoiceId}, CustomerId: {CustomerId}, Reason: {Reason}",
					model?.InvoiceId, model?.CustomerId, model?.RefundReason);

				// ✅ Validate dữ liệu đầu vào
				if (model == null)
				{
					_logger.LogWarning("CREATE_REFUND_INVALID_MODEL: Model rỗng");
					return false;
				}

				if (!model.InvoiceId.HasValue || model.InvoiceId <= 0)
				{
					_logger.LogWarning("CREATE_REFUND_INVALID_INVOICE_ID: InvoiceId không hợp lệ - InvoiceId: {InvoiceId}",
						model.InvoiceId);
					return false;
				}

				if (!model.CustomerId.HasValue || model.CustomerId <= 0)
				{
					_logger.LogWarning("CREATE_REFUND_INVALID_CUSTOMER_ID: CustomerId không hợp lệ - CustomerId: {CustomerId}",
						model.CustomerId);
					return false;
				}

				// ✅ Kiểm tra hóa đơn có tồn tại không
				var invoice = await _invoiceRepository.GetById(model.InvoiceId.Value);
				if (invoice == null || invoice.DeleteStatus)
				{
					_logger.LogWarning("CREATE_REFUND_INVOICE_NOT_FOUND: Hóa đơn không tồn tại - InvoiceId: {InvoiceId}",
						model.InvoiceId);
					return false;
				}

				// ✅ Kiểm tra khách hàng có tồn tại không
				var customer = await _customerRepository.GetById(model.CustomerId.Value);
				if (customer == null || customer.DeleteStatus)
				{
					_logger.LogWarning("CREATE_REFUND_CUSTOMER_NOT_FOUND: Khách hàng không tồn tại - CustomerId: {CustomerId}",
						model.CustomerId);
					return false;
				}

				// ✅ Kiểm tra khách hàng không có yêu cầu hoàn tiền nào đang chờ xử lý
				var existingRefund = await _refundRepository.FindByPredicate(
					x => x.InvoiceId == model.InvoiceId &&
						 (x.Status == "PendingApproval" || x.Status == "Approved") &&
						 !x.DeleteStatus);

				if (existingRefund != null && existingRefund.Count > 0)
				{
					_logger.LogWarning("CREATE_REFUND_ALREADY_PENDING: Hóa đơn đã có yêu cầu hoàn tiền đang chờ xử lý - InvoiceId: {InvoiceId}",
						model.InvoiceId);
					return false;
				}

				// ✅ Lấy số tiền hoàn từ hóa đơn
				decimal refundAmount = invoice.PaidAmount ?? 0;

				// ✅ Lấy thông tin tài khoản ngân hàng mặc định của khách hàng
				var defaultPaymentInfo = await _customerPaymentInfoRepository.FindByPredicate(
					x => x.CustomerId == model.CustomerId && x.IsDefault && !x.DeleteStatus);

				string refundImages = model.RefundImages;
				if (!string.IsNullOrEmpty(model.RefundImages))
				{
					refundImages = await _commonService.BaseProcessingFunction64(model.RefundImages);
				}

				// ✅ Tạo entity hoàn tiền
				var refund = new RefundEntity
				{
					InvoiceId = model.InvoiceId,
					CustomerId = model.CustomerId,
					RefundAmount = refundAmount,
					RefundReason = model.RefundReason?.Trim(),
					RefundImages = refundImages,
					RefundMethod = model.RefundMethod?.Trim().ToUpper() ?? "BANKTRANSFER",
					Status = "PendingApproval",
					CreatedDate = DateTime.UtcNow,
					DeleteStatus = false
				};

				// ✅ Nếu có tài khoản ngân hàng mặc định, lưu thông tin
				if (defaultPaymentInfo != null && defaultPaymentInfo.Count > 0)
				{
					var paymentInfo = defaultPaymentInfo.First();
					refund.BankAccount = paymentInfo.BankAccountNumber;
					refund.BankAccountName = paymentInfo.BankAccountName;
					refund.BankName = paymentInfo.BankName;

					_logger.LogInformation("CREATE_REFUND_BANK_INFO_FOUND: Lấy thông tin tài khoản mặc định - CustomerId: {CustomerId}, BankCode: {BankCode}",
						model.CustomerId, paymentInfo.BankCode);
				}

				// ✅ Lưu vào database
				bool result = await _refundRepository.CreateEntity(refund);

				if (result)
				{
					invoice.IsRefund = true;
					await _invoiceRepository.UpdateEntity(invoice);
					_logger.LogInformation("CREATE_REFUND_SUCCESS: Tạo yêu cầu hoàn tiền thành công - RefundId: {RefundId}, InvoiceId: {InvoiceId}, Amount: {Amount:C}",
						refund.Id, model.InvoiceId, refundAmount);
				}
				else
				{
					_logger.LogError("CREATE_REFUND_FAILED: Tạo yêu cầu hoàn tiền thất bại - InvoiceId: {InvoiceId}",
						model.InvoiceId);
				}

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CREATE_REFUND_EXCEPTION: Lỗi khi tạo yêu cầu hoàn tiền - InvoiceId: {InvoiceId}",
					model?.InvoiceId);
				return false;
			}
		}

		/// <summary>
		/// ✅ Cập nhật trạng thái hoàn tiền
		/// </summary>
		public async Task<bool> updaterefundservice(UpdtaeRefundModel model)
		{
			try
			{
				_logger.LogInformation("UPDATE_REFUND_START: Cập nhật trạng thái hoàn tiền - RefundId: {RefundId}, Status: {Status}",
					model?.Id, model?.Status);

				// ✅ Validate dữ liệu đầu vào
				if (model == null)
				{
					_logger.LogWarning("UPDATE_REFUND_INVALID_MODEL: Model rỗng");
					return false;
				}

				if (!model.Id.HasValue || model.Id <= 0)
				{
					_logger.LogWarning("UPDATE_REFUND_INVALID_ID: RefundId không hợp lệ - Id: {Id}",
						model.Id);
					return false;
				}

				if (string.IsNullOrWhiteSpace(model.Status))
				{
					_logger.LogWarning("UPDATE_REFUND_INVALID_STATUS: Trạng thái không hợp lệ - Status: {Status}",
						model.Status);
					return false;
				}

				// ✅ Validate trạng thái hợp lệ
				var validStatuses = new[] { "PendingApproval", "Approved", "Rejected" };
				if (!validStatuses.Contains(model.Status))
				{
					_logger.LogWarning("UPDATE_REFUND_INVALID_STATUS_VALUE: Trạng thái không được hỗ trợ - Status: {Status}",
						model.Status);
					return false;
				}

				// ✅ Lấy thông tin hoàn tiền hiện tại
				var existingRefund = await _refundRepository.GetById(model.Id.Value);
				if (existingRefund == null || existingRefund.DeleteStatus)
				{
					_logger.LogWarning("UPDATE_REFUND_NOT_FOUND: Yêu cầu hoàn tiền không tồn tại - RefundId: {RefundId}",
						model.Id);
					return false;
				}

				var oldStatus = existingRefund.Status;
				existingRefund.Status = model.Status;
				existingRefund.StaffId = model.StaffId;

				// ✅ NẾU CHUYỂN SANG "APPROVED" THÌ GỌI HÀMXỬ LÝ HOÀN TIỀN
				if (model.Status == "Approved")
				{
					_logger.LogInformation("UPDATE_REFUND_TRIGGER_HANDLER: Kích hoạt xử lý hoàn tiền - RefundId: {RefundId}",
						model.Id);

					bool processResult = await HandleApprovedRefund(existingRefund);
					if (!processResult)
					{
						_logger.LogError("UPDATE_REFUND_HANDLE_FAILED: Xử lý hoàn tiền thất bại - RefundId: {RefundId}",
							model.Id);
						return false;
					}
				}
				else if (model.Status == "Rejected")
				{
					existingRefund.ApprovedDate = null;
					var invoice = await _invoiceRepository.GetById(existingRefund.InvoiceId.Value);
					invoice.Status = "HuyHoanTien";
					await _invoiceRepository.UpdateEntity(invoice);
					_logger.LogInformation("UPDATE_REFUND_REJECTED: Yêu cầu hoàn tiền bị từ chối - RefundId: {RefundId}, Status: {Status}",
						model.Id, model.Status);
				}

				// ✅ Lưu vào database
				bool result = await _refundRepository.UpdateEntity(existingRefund);

				if (!result)
				{
					_logger.LogError("UPDATE_REFUND_SAVE_FAILED: Cập nhật RefundEntity thất bại - RefundId: {RefundId}",
						model.Id);
					return false;
				}

				_logger.LogInformation("UPDATE_REFUND_SUCCESS: Cập nhật trạng thái hoàn tiền thành công - RefundId: {RefundId}, OldStatus: {OldStatus}, NewStatus: {NewStatus}",
					model.Id, oldStatus, model.Status);

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_REFUND_EXCEPTION: Lỗi khi cập nhật trạng thái hoàn tiền - RefundId: {RefundId}",
					model?.Id);
				return false;
			}
		}

		/// <summary>
		/// Lấy danh sách yêu cầu hoàn tiền với các filter và phân trang
		/// </summary>
		public async Task<BaseDataCollection<RefundEntity>> getlistrefund(getlist model)
		{
			try
			{
				_logger.LogInformation("GET_LIST_REFUND_START: Lấy danh sách hoàn tiền - InvoiceId: {InvoiceId}, CustomerId: {CustomerId}",
					model?.InvoiceId, model?.CustomerId);

				var result = new BaseDataCollection<RefundEntity>();

				// ✅ Validate dữ liệu đầu vào
				if (model == null)
				{
					_logger.LogWarning("GET_LIST_REFUND_INVALID_MODEL: Model rỗng");
					result.BaseDatas = new List<RefundEntity>();
					result.TotalRecordCount = 0;
					return result;
				}

				// ✅ Validate pagination parameters
				if (model.PageNo <= 0 || model.PageSize <= 0)
				{
					_logger.LogWarning("GET_LIST_REFUND_INVALID_PAGINATION: Tham số phân trang không hợp lệ - PageNo: {PageNo}, PageSize: {PageSize}",
						model.PageNo, model.PageSize);
					result.BaseDatas = new List<RefundEntity>();
					result.TotalRecordCount = 0;
					result.PageIndex = model.PageNo;
					result.PageCount = 0;
					return result;
				}

				// ✅ Xây dựng filter predicate
				Expression<Func<RefundEntity, bool>> predicate = x => !x.DeleteStatus;

				// ✅ Filter theo InvoiceId
				if (model.InvoiceId.HasValue && model.InvoiceId > 0)
				{
					predicate = predicate.And(x => x.InvoiceId == model.InvoiceId);
				}

				// ✅ Filter theo CustomerId
				if (model.CustomerId.HasValue && model.CustomerId > 0)
				{
					predicate = predicate.And(x => x.CustomerId == model.CustomerId);
				}

				// ✅ Filter theo StaffId
				if (model.StaffId.HasValue && model.StaffId > 0)
				{
					predicate = predicate.And(x => x.StaffId == model.StaffId);
				}

				// ✅ Lấy danh sách
				var refunds = await _refundRepository.FindByPredicate(predicate);

				if (refunds == null || refunds.Count == 0)
				{
					_logger.LogInformation("GET_LIST_REFUND_EMPTY: Không tìm thấy yêu cầu hoàn tiền nào");
					result.BaseDatas = new List<RefundEntity>();
					result.TotalRecordCount = 0;
					result.PageIndex = model.PageNo;
					result.PageCount = 0;
					return result;
				}

				// ✅ Sắp xếp: mới nhất trước
				var sortedRefunds = refunds
					.OrderByDescending(x => x.CreatedDate)
					.ToList();

				// ✅ Phân trang
				int totalRecords = sortedRefunds.Count;
				int pageSize = model.PageSize;
				int pageNo = model.PageNo;
				int totalPages = (int)Math.Ceiling((decimal)totalRecords / pageSize);

				var pagedRefunds = sortedRefunds
					.Skip((pageNo - 1) * pageSize)
					.Take(pageSize)
					.ToList();

				result.BaseDatas = pagedRefunds;
				result.TotalRecordCount = totalRecords;
				result.PageIndex = pageNo;
				result.PageCount = totalPages;

				_logger.LogInformation("GET_LIST_REFUND_SUCCESS: Lấy danh sách hoàn tiền thành công - TotalCount: {TotalCount}, PageNo: {PageNo}, PageSize: {PageSize}",
					totalRecords, pageNo, pageSize);

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_LIST_REFUND_EXCEPTION: Lỗi khi lấy danh sách hoàn tiền");

				return new BaseDataCollection<RefundEntity>
				{
					BaseDatas = new List<RefundEntity>(),
					TotalRecordCount = 0,
					PageIndex = model?.PageNo ?? 1,
					PageCount = 0
				};
			}
		}


		#region Private Methods

		/// <summary>
		/// ✅ PRIVATE METHOD: Xử lý hoàn tiền khi status = "Approved"
		/// </summary>
		private async Task<bool> HandleApprovedRefund(RefundEntity refund)
		{
			try
			{
				_logger.LogInformation("HANDLE_APPROVED_REFUND_START: Bắt đầu xử lý hoàn tiền - RefundId: {RefundId}, InvoiceId: {InvoiceId}, CustomerId: {CustomerId}",
					refund.Id, refund.InvoiceId, refund.CustomerId);

				// ✅ BƯỚC 1: Kiểm tra InvoiceId
				if (!refund.InvoiceId.HasValue)
				{
					_logger.LogWarning("HANDLE_APPROVED_REFUND_NO_INVOICE_ID: InvoiceId không hợp lệ - RefundId: {RefundId}",
						refund.Id);
					return false;
				}

				// ✅ BƯỚC 2: Lấy thông tin hóa đơn
				var invoice = await _invoiceRepository.GetById(refund.InvoiceId.Value);
				if (invoice == null || invoice.DeleteStatus)
				{
					_logger.LogWarning("HANDLE_APPROVED_REFUND_INVOICE_NOT_FOUND: Hóa đơn không tồn tại - InvoiceId: {InvoiceId}",
						refund.InvoiceId);
					return false;
				}

				string refundMethod = refund.RefundMethod ?? "";

				// ✅ TRƯỜNG HỢP 1: HOÀN TIỀN BẰNG TIỀN MẶT
				if (refundMethod.Equals("TIENMAT", StringComparison.OrdinalIgnoreCase))
				{
					_logger.LogInformation("HANDLE_APPROVED_REFUND_CASH_METHOD: Hoàn tiền bằng tiền mặt - RefundId: {RefundId}, Amount: {Amount:C}",
						refund.Id, refund.RefundAmount);

					refund.ApprovedDate = DateTime.UtcNow;
					refund.CompletedDate = DateTime.UtcNow;

					_logger.LogInformation("HANDLE_APPROVED_REFUND_CASH_ENTITY_UPDATED: RefundEntity được cập nhật (tiền mặt) - RefundId: {RefundId}, ApprovedDate: {ApprovedDate}",
						refund.Id, refund.ApprovedDate);

					return true;
				}

				// ✅ BƯỚC 3: Kiểm tra TransactionId từ Invoice
				if (string.IsNullOrEmpty(invoice.TransactionId))
				{
					_logger.LogWarning("HANDLE_APPROVED_REFUND_NO_TRANSACTION_ID: TransactionId không tồn tại - InvoiceId: {InvoiceId}",
						refund.InvoiceId);
					return false;
				}

				// ✅ BƯỚC 4: Kiểm tra CustomerId
				if (!refund.CustomerId.HasValue)
				{
					_logger.LogWarning("HANDLE_APPROVED_REFUND_NO_CUSTOMER_ID: CustomerId không hợp lệ - RefundId: {RefundId}",
						refund.Id);
					return false;
				}

				// ✅ BƯỚC 5: Lấy thông tin khách hàng
				var customer = await _customerRepository.GetById(refund.CustomerId.Value);
				if (customer == null || customer.DeleteStatus)
				{
					_logger.LogWarning("HANDLE_APPROVED_REFUND_CUSTOMER_NOT_FOUND: Khách hàng không tồn tại - CustomerId: {CustomerId}",
						refund.CustomerId);
					return false;
				}

				// ✅ BƯỚC 6: LẤY TÀI KHOẢN NGÂN HÀNG MẶC ĐỊNH (IsDefault = true)
				var defaultPaymentInfo = await _customerPaymentInfoRepository.FindByPredicate(
					x => x.CustomerId == refund.CustomerId && x.IsDefault && !x.DeleteStatus);

				if (defaultPaymentInfo == null || defaultPaymentInfo.Count == 0)
				{
					_logger.LogWarning("HANDLE_APPROVED_REFUND_NO_DEFAULT_ACCOUNT: Khách hàng không có tài khoản mặc định - CustomerId: {CustomerId}",
						refund.CustomerId);
					return false;
				}

				var paymentInfo = defaultPaymentInfo.First();

				_logger.LogInformation("HANDLE_APPROVED_REFUND_BANK_INFO_FOUND: Lấy tài khoản mặc định thành công - CustomerId: {CustomerId}, BankCode: {BankCode}, BankAccount: {BankAccount}",
					refund.CustomerId, paymentInfo.BankCode, paymentInfo.BankAccountNumber);

				// ✅ BƯỚC 7: LẤY SỐ TIỀN HOÀN
				decimal refundAmount = invoice.PaidAmount ?? 0;

				_logger.LogInformation("HANDLE_APPROVED_REFUND_PREPARE_DATA: Chuẩn bị dữ liệu hoàn tiền - RefundId: {RefundId}, Amount: {Amount:C}, OriginalTransactionId: {TransactionId}, BankCode: {BankCode}",
					refund.Id, refundAmount, invoice.TransactionId, paymentInfo.BankCode);

				// ✅ BƯỚC 8: GỌI API VNPAY VÀ LẤY TRANSACTION ID
				string refundTransactionId = await ProcessVNPayRefund(
					refund.Id,
					refund.InvoiceId.Value,
					invoice.TransactionId,
					refundAmount,
					refund.RefundReason);

				if (string.IsNullOrEmpty(refundTransactionId))
				{
					_logger.LogError("HANDLE_APPROVED_REFUND_API_FAILED: Gọi API VNPay hoàn tiền thất bại - RefundId: {RefundId}",
						refund.Id);
					return false;
				}

				// ✅ BƯỚC 9: CẬP NHẬT REFUNDENTITY VỚI TRANSACTION ID TỪ VNPAY
				refund.ApprovedDate = DateTime.UtcNow;
				refund.CompletedDate = DateTime.UtcNow;
				refund.RefundTransactionId = refundTransactionId;
				refund.BankAccount = paymentInfo.BankAccountNumber;
				refund.BankAccountName = paymentInfo.BankAccountName;
				refund.BankName = paymentInfo.BankName;

				_logger.LogInformation("HANDLE_APPROVED_REFUND_ENTITY_UPDATED: RefundEntity được cập nhật - RefundId: {RefundId}, ApprovedDate: {ApprovedDate}, CompletedDate: {CompletedDate}",
					refund.Id, refund.ApprovedDate, refund.CompletedDate);

				// ✅ BƯỚC 10: CẬP NHẬT INVOICEENTITY
				invoice.Status = "DaHoanTien";

				bool invoiceUpdateResult = await _invoiceRepository.UpdateEntity(invoice);
				if (!invoiceUpdateResult)
				{
					_logger.LogWarning("HANDLE_APPROVED_REFUND_INVOICE_UPDATE_FAILED: Cập nhật InvoiceEntity thất bại - InvoiceId: {InvoiceId}",
						refund.InvoiceId);
				}

				// ✅ BƯỚC 11: TRỪ ĐIỂM RATING KHI HOÀN TIỀN
				bool pointsSubtracted = await SubtractRatingPointsAsync(refund.CustomerId.Value, refundAmount);
				if (!pointsSubtracted)
				{
					_logger.LogWarning("HANDLE_APPROVED_REFUND_POINTS_SUBTRACT_FAILED: Trừ điểm rating thất bại - CustomerId: {CustomerId}",
						refund.CustomerId);
				}

				_logger.LogInformation("HANDLE_APPROVED_REFUND_SUCCESS: Xử lý hoàn tiền thành công - RefundId: {RefundId}, InvoiceId: {InvoiceId}, Amount: {Amount:C}",
					refund.Id, refund.InvoiceId, refundAmount);

				_logger.LogInformation("HANDLE_APPROVED_REFUND_SUCCESS: Xử lý hoàn tiền thành công - RefundId: {RefundId}, InvoiceId: {InvoiceId}, Amount: {Amount:C}",
					refund.Id, refund.InvoiceId, refundAmount);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "HANDLE_APPROVED_REFUND_EXCEPTION: Lỗi khi xử lý hoàn tiền - RefundId: {RefundId}",
					refund?.Id);
				return false;
			}
		}

		/// <summary>
		/// ✅ Gọi API VNPay để hoàn tiền
		/// </summary>
		//private async Task<string> ProcessVNPayRefund(
		//	int refundId,
		//	int invoiceId,
		//	string originalTransactionId,
		//	decimal refundAmount,
		//	string refundReason)
		//{
		//	try
		//	{
		//		_logger.LogInformation("PROCESS_VNPAY_REFUND_START: RefundId={RefundId}, Amount={Amount}", refundId, refundAmount);

		//		string vnp_TmnCode = _configuration.GetSection("Vnpay")["TmnCode"];
		//		string vnp_HashSecret = _configuration.GetSection("Vnpay")["HashSecret"];
		//		string vnp_ApiUrl = _configuration.GetSection("Vnpay")["RefundApiUrl"];

		//		_logger.LogInformation("VNPAY_CONFIG: TmnCode={TmnCode}, HashSecret={SecretLength}chars, ApiUrl={ApiUrl}",
		//			vnp_TmnCode, vnp_HashSecret?.Length ?? 0, vnp_ApiUrl);

		//		if (string.IsNullOrEmpty(vnp_TmnCode) || string.IsNullOrEmpty(vnp_HashSecret))
		//		{
		//			_logger.LogError("CONFIG_MISSING: TmnCode or HashSecret is empty");
		//			return null;
		//		}

		//		if (refundAmount <= 0)
		//		{
		//			_logger.LogError("INVALID_REFUND_AMOUNT: Amount must be > 0 - Amount={Amount}", refundAmount);
		//			return null;
		//		}

		//		// ✅ Lấy invoice để có thời gian thanh toán gốc
		//		var invoice = await _invoiceRepository.GetById(invoiceId);
		//		if (invoice == null)
		//		{
		//			_logger.LogError("INVOICE_NOT_FOUND: InvoiceId={InvoiceId}", invoiceId);
		//			return null;
		//		}

		//		// ✅ Tạo parameters
		//		string vnp_RequestId = $"REFUND_{refundId}_{DateTime.UtcNow.Ticks}";
		//		string vnp_Version = "2.1.0";
		//		string vnp_Command = "refund";
		//		string vnp_TransactionType = "02";
		//		string vnp_TxnRef = originalTransactionId;
		//		long vnp_Amount = (long)(refundAmount * 100);

		//		string sanitizedReason = System.Text.RegularExpressions.Regex.Replace(
		//			refundReason ?? "Refund",
		//			@"[^\x20-\x7E]",
		//			"");
		//		string vnp_OrderInfo = $"REFUND_{refundId}";

		//		string vnp_CreateBy = "System";

		//		string vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss");

		//		string vnp_IpAddr = await GetServerIpAddress();

		//		if (!invoice.PaymentDate.HasValue)
		//		{
		//			_logger.LogError("PAYMENT_DATE_MISSING: Invoice không có PaymentDate - InvoiceId={InvoiceId}", invoiceId);
		//			return null;
		//		}
		//		string vnp_TransactionDate = invoice.PaymentDate.Value.ToString("yyyyMMddHHmmss");

		//		_logger.LogInformation("DEBUG_DATES: PaymentDate_FromDB={PaymentDate}, PaymentDate_Formatted={PaymentDateFormatted}, CreateDate_Formatted={CreateDateFormatted}",
		//			invoice.PaymentDate.Value,
		//			vnp_TransactionDate,
		//			vnp_CreateDate);

		//		// ✅ BUILD HASH DATA - EXACT ORDER FROM VNPAY DOCS
		//		string hashData = $"{vnp_RequestId}|{vnp_Version}|{vnp_Command}|{vnp_TmnCode}|{vnp_TransactionType}|{vnp_TxnRef}|{vnp_Amount}||{vnp_TransactionDate}|{vnp_CreateBy}|{vnp_CreateDate}|{vnp_IpAddr}|{vnp_OrderInfo}";

		//		_logger.LogInformation("HASH_DATA_RAW: {Data}", hashData);

		//		string vnp_SecureHash = ComputeHmacSHA512(hashData, vnp_HashSecret);
		//		_logger.LogInformation("HASH_COMPUTED: {Hash}", vnp_SecureHash);


		//		// Thêm vào hàm ProcessVNPayRefund, trước khi tính hash:
		//		_logger.LogInformation("=== HASH SECRET DEBUG ===");
		//		_logger.LogInformation("Secret from Config: {Secret}", vnp_HashSecret);
		//		_logger.LogInformation("Secret Length: {Length}", vnp_HashSecret.Length);
		//		_logger.LogInformation("Secret Bytes (HEX): {Hex}",
		//		string.Join(" ", System.Text.Encoding.UTF8.GetBytes(vnp_HashSecret)
		//			.Select(b => b.ToString("X2"))));
		//		_logger.LogInformation("=== END DEBUG ===");

		//		var requestBody = new
		//		{
		//			vnp_RequestId,
		//			vnp_Version,
		//			vnp_Command,
		//			vnp_TmnCode,
		//			vnp_TransactionType,
		//			vnp_TxnRef,
		//			vnp_Amount,
		//			vnp_TransactionDate,
		//			vnp_CreateBy,
		//			vnp_CreateDate,
		//			vnp_IpAddr,
		//			vnp_OrderInfo,
		//			vnp_SecureHash
		//		};

		//		var jsonRequest = System.Text.Json.JsonSerializer.Serialize(requestBody, new System.Text.Json.JsonSerializerOptions
		//		{
		//			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		//		});
		//		_logger.LogInformation("REQUEST_JSON: {Json}", jsonRequest);

		//		using (var httpClient = new HttpClient())
		//		{
		//			httpClient.Timeout = TimeSpan.FromSeconds(10);
		//			var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
		//			httpClient.DefaultRequestHeaders.Add("User-Agent", "Aesthetics-Refund-Service/1.0");

		//			_logger.LogInformation("CALLING_VNPAY: POST {Url}", vnp_ApiUrl);

		//			var response = await httpClient.PostAsync(vnp_ApiUrl, content);
		//			var responseContent = await response.Content.ReadAsStringAsync();

		//			_logger.LogInformation("VNPAY_RESPONSE: StatusCode={StatusCode}, Body={Body}", response.StatusCode, responseContent);

		//			if (!response.IsSuccessStatusCode)
		//			{
		//				_logger.LogError("VNPAY_HTTP_ERROR: StatusCode={StatusCode}", response.StatusCode);
		//				return null;
		//			}

		//			try
		//			{
		//				var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
		//				var root = jsonDoc.RootElement;

		//				if (root.TryGetProperty("vnp_ResponseCode", out var responseCode))
		//				{
		//					string code = responseCode.GetString();
		//					_logger.LogInformation("VNPAY_RESPONSE_CODE: {Code}", code);

		//					_logger.LogInformation("✅ VNPAY_SUCCESS");
		//					if (root.TryGetProperty("vnp_TransactionNo", out var txnNo))
		//						return txnNo.GetString();
		//					if (root.TryGetProperty("vnp_ResponseId", out var respId))
		//						return respId.GetString();
		//					return "SUCCESS";

		//					//if (code == "00")
		//					//{
		//					//	_logger.LogInformation("✅ VNPAY_SUCCESS");
		//					//	if (root.TryGetProperty("vnp_TransactionNo", out var txnNo))
		//					//		return txnNo.GetString();
		//					//	if (root.TryGetProperty("vnp_ResponseId", out var respId))
		//					//		return respId.GetString();
		//					//	return "SUCCESS";
		//					//}
		//					//else
		//					//{
		//					//	string msg = "Unknown";
		//					//	if (root.TryGetProperty("vnp_Message", out var msgProp))
		//					//		msg = msgProp.GetString();
		//					//	_logger.LogError("❌ VNPAY_FAILED: Code={Code}, Message={Message}", code, msg);
		//					//	return null;
		//					//}
		//				}

		//				_logger.LogError("❌ NO_RESPONSE_CODE");
		//				return null;
		//			}
		//			catch (Exception ex)
		//			{
		//				_logger.LogError(ex, "❌ PARSE_RESPONSE_ERROR");
		//				return null;
		//			}
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		_logger.LogError(ex, "❌ EXCEPTION: RefundId={RefundId}", refundId);
		//		return null;
		//	}
		//}

		private async Task<string> ProcessVNPayRefund(
			int refundId,
			int invoiceId,
			string originalTransactionId,
			decimal refundAmount,
			string refundReason)
		{
			try
			{
				_logger.LogInformation("🔄 PROCESS_VNPAY_REFUND_START: RefundId={RefundId}, Amount={Amount:C}, TxnRef={TxnRef}",
					refundId, refundAmount, originalTransactionId);

				string vnp_TmnCode = _configuration.GetSection("Vnpay")["TmnCode"];
				string vnp_HashSecret = _configuration.GetSection("Vnpay")["HashSecret"];
				string vnp_ApiUrl = _configuration.GetSection("Vnpay")["RefundApiUrl"];

				_logger.LogInformation("⚙️ VNPAY_CONFIG: TmnCode={TmnCode}, HashSecretLength={SecretLength}, ApiUrl={ApiUrl}",
					vnp_TmnCode, vnp_HashSecret?.Length ?? 0, vnp_ApiUrl);

				// ✅ Validate config
				if (string.IsNullOrEmpty(vnp_TmnCode) || string.IsNullOrEmpty(vnp_HashSecret))
				{
					_logger.LogError("❌ CONFIG_MISSING: TmnCode or HashSecret is empty");
					return null;
				}

				// ✅ Validate refund amount
				if (refundAmount <= 0)
				{
					_logger.LogError("❌ INVALID_REFUND_AMOUNT: Amount must be > 0 - Amount={Amount}", refundAmount);
					return null;
				}

				// ✅ Lấy invoice
				var invoice = await _invoiceRepository.GetById(invoiceId);
				if (invoice == null)
				{
					_logger.LogError("❌ INVOICE_NOT_FOUND: InvoiceId={InvoiceId}", invoiceId);
					return null;
				}

				if (!invoice.PaymentDate.HasValue)
				{
					_logger.LogError("❌ PAYMENT_DATE_MISSING: Invoice không có PaymentDate - InvoiceId={InvoiceId}", invoiceId);
					return null;
				}

				// ✅ Tạo parameters
				string vnp_RequestId = $"REFUND_{refundId}_{DateTime.UtcNow.Ticks}";
				string vnp_Version = "2.1.0";
				string vnp_Command = "refund";
				string vnp_TransactionType = "02";
				string vnp_TxnRef = originalTransactionId;
				long vnp_Amount = (long)(refundAmount * 100);

				string vnp_OrderInfo = $"REFUND_{refundId}";
				string vnp_CreateBy = "System";
				string vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss");
				string vnp_IpAddr = await GetServerIpAddress();
				string vnp_TransactionDate = invoice.PaymentDate.Value.ToString("yyyyMMddHHmmss");

				_logger.LogInformation("📝 REQUEST_PARAMS: RequestId={RequestId}, Amount={Amount} xu, TxnRef={TxnRef}",
					vnp_RequestId, vnp_Amount, vnp_TxnRef);

				// ✅ BUILD HASH DATA
				string hashData = $"{vnp_RequestId}|{vnp_Version}|{vnp_Command}|{vnp_TmnCode}|{vnp_TransactionType}|{vnp_TxnRef}|{vnp_Amount}||{vnp_TransactionDate}|{vnp_CreateBy}|{vnp_CreateDate}|{vnp_IpAddr}|{vnp_OrderInfo}";

				string vnp_SecureHash = ComputeHmacSHA512(hashData, vnp_HashSecret);

				_logger.LogInformation("🔐 HASH_COMPUTED: {Hash}", vnp_SecureHash);

				var requestBody = new
				{
					vnp_RequestId,
					vnp_Version,
					vnp_Command,
					vnp_TmnCode,
					vnp_TransactionType,
					vnp_TxnRef,
					vnp_Amount,
					vnp_TransactionDate,
					vnp_CreateBy,
					vnp_CreateDate,
					vnp_IpAddr,
					vnp_OrderInfo,
					vnp_SecureHash
				};

				var jsonRequest = System.Text.Json.JsonSerializer.Serialize(requestBody, new System.Text.Json.JsonSerializerOptions
				{
					Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
				});

				_logger.LogInformation("📤 REQUEST_JSON: {Json}", jsonRequest);

				// ✅ RETRY LOGIC - Thử 3 lần nếu timeout
				int maxRetries = 3;
				int retryCount = 0;
				TimeSpan timeout = TimeSpan.FromSeconds(30); // 🆕 Tăng từ 10s lên 30s

				while (retryCount < maxRetries)
				{
					try
					{
						retryCount++;
						_logger.LogInformation("📤 CALLING_VNPAY_API: Attempt {Attempt}/{MaxRetries}, Timeout={Timeout}s, URL={Url}",
							retryCount, maxRetries, timeout.TotalSeconds, vnp_ApiUrl);

						using (var httpClient = new HttpClient())
						{
							httpClient.Timeout = timeout;
							var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
							httpClient.DefaultRequestHeaders.Add("User-Agent", "Aesthetics-Refund-Service/1.0");

							var startTime = DateTime.UtcNow;
							var response = await httpClient.PostAsync(vnp_ApiUrl, content);
							var elapsedTime = DateTime.UtcNow - startTime;

							var responseContent = await response.Content.ReadAsStringAsync();

							_logger.LogInformation("📥 VNPAY_RESPONSE: StatusCode={StatusCode}, ElapsedTime={ElapsedMs}ms, Body={Body}",
								response.StatusCode, elapsedTime.TotalMilliseconds, responseContent);

							if (!response.IsSuccessStatusCode)
							{
								_logger.LogError("❌ HTTP_ERROR: StatusCode={StatusCode}", response.StatusCode);

								// Retry nếu là server error
								if ((int)response.StatusCode >= 500 && retryCount < maxRetries)
								{
									_logger.LogWarning("⚠️ Server error, retrying... ({Current}/{Max})", retryCount, maxRetries);
									await Task.Delay(2000); // Đợi 2 giây trước retry
									continue;
								}
								return null;
							}

							try
							{
								var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
								var root = jsonDoc.RootElement;

								if (root.TryGetProperty("vnp_ResponseCode", out var responseCode))
								{
									string code = responseCode.GetString();

									_logger.LogInformation("✅ VNPAY_RESPONSE_CODE: {Code}", code);

									if (code == "00")
									{
										_logger.LogInformation("✅ VNPAY_SUCCESS: Hoàn tiền thành công");
										if (root.TryGetProperty("vnp_TransactionNo", out var txnNo))
											return txnNo.GetString();
										if (root.TryGetProperty("vnp_ResponseId", out var respId))
											return respId.GetString();
										return "SUCCESS";
									}
									else
									{
										string msg = "Unknown";
										if (root.TryGetProperty("vnp_Message", out var msgProp))
											msg = msgProp.GetString();

										_logger.LogError("❌ VNPAY_FAILED: Code={Code}, Message={Message}", code, msg);

										// Không retry nếu lỗi logic (93, 91)
										if (code == "93" || code == "91" || code == "94")
										{
											_logger.LogError("❌ LOGIC_ERROR - Không retry: {Code}", code);
											return null;
										}

										// Retry nếu lỗi khác
										if (retryCount < maxRetries)
										{
											_logger.LogWarning("⚠️ Retrying due to error code {Code}... ({Current}/{Max})", code, retryCount, maxRetries);
											await Task.Delay(2000);
											continue;
										}

										return null;
									}
								}

								_logger.LogError("❌ NO_RESPONSE_CODE");
								return null;
							}
							catch (Exception ex)
							{
								_logger.LogError(ex, "❌ PARSE_RESPONSE_ERROR");
								return null;
							}
						}
					}
					catch (TaskCanceledException ex)
					{
						_logger.LogWarning(ex, "⏱️ TIMEOUT_ERROR: Attempt {Current}/{Max} - {Message}",
							retryCount, maxRetries, ex.Message);

						if (retryCount < maxRetries)
						{
							_logger.LogInformation("⏱️ Timeout - Retrying after 2 seconds... ({Current}/{Max})", retryCount, maxRetries);
							await Task.Delay(2000);
							continue;
						}
						else
						{
							_logger.LogError("❌ TIMEOUT_FAILED_ALL_RETRIES: All {MaxRetries} attempts timed out", maxRetries);
							return null;
						}
					}
					catch (HttpRequestException ex)
					{
						_logger.LogError(ex, "❌ HTTP_REQUEST_ERROR: Attempt {Current}/{Max}",
							retryCount, maxRetries);

						if (retryCount < maxRetries)
						{
							_logger.LogWarning("⚠️ HTTP error - Retrying... ({Current}/{Max})", retryCount, maxRetries);
							await Task.Delay(2000);
							continue;
						}

						return null;
					}
				}

				_logger.LogError("❌ MAX_RETRIES_EXCEEDED: Failed after {MaxRetries} attempts", maxRetries);
				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ PROCESS_VNPAY_REFUND_EXCEPTION: RefundId={RefundId}", refundId);
				return null;
			}
		}

		private string ComputeHmacSHA512(string input, string secretKey)
		{
			byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
			byte[] inputBytes = Encoding.UTF8.GetBytes(input);

			using (var hmac = new System.Security.Cryptography.HMACSHA512(keyBytes))
			{
				byte[] hash = hmac.ComputeHash(inputBytes);
				return BitConverter.ToString(hash).Replace("-", "").ToLower();
			}
		}

		private async Task<string> GetServerIpAddress()
		{
			try
			{
				using (var httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) })
				{
					try
					{
						var response = await httpClient.GetAsync("https://api.ipify.org");
						if (response.IsSuccessStatusCode)
						{
							var publicIp = await response.Content.ReadAsStringAsync();
							if (!string.IsNullOrWhiteSpace(publicIp))
							{
								_logger.LogInformation("GET_SERVER_IP: Public IP from ipify - IP: {IP}", publicIp.Trim());
								return publicIp.Trim();
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "GET_SERVER_IP: Không thể lấy public IP từ ipify");
					}
				}

				var hostName = System.Net.Dns.GetHostName();
				var addresses = System.Net.Dns.GetHostAddresses(hostName);

				foreach (var address in addresses)
				{
					if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
					{
						var ipAddress = address.ToString();

						if (!ipAddress.StartsWith("127."))
						{
							_logger.LogWarning("GET_SERVER_IP: Sử dụng IPv4 local - IP: {IP} (⚠️ CẦN WHITELIST tại VNPay Dashboard)", ipAddress);
							return ipAddress;
						}
					}
				}

				_logger.LogError("GET_SERVER_IP: Không tìm thấy IP hợp lệ, sử dụng 127.0.0.1 (⚠️ SẼ GẶP LỖI - CẦN WHITELIST)");
				return "127.0.0.1";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_SERVER_IP_ADDRESS: Lỗi khi lấy địa chỉ IP");
				return "127.0.0.1";
			}
		}

		#endregion

		#region Logic Update RatingPoints
		/// <summary>
		/// ✅ Tính điểm rating khi hóa đơn thanh toán thành công
		/// Công thức: 10 điểm / 1,000,000 VNĐ
		/// </summary>
		public async Task<bool> AddRatingPointsAsync(int customerId, decimal invoiceAmount)
		{
			try
			{
				_logger.LogInformation("ADD_RATING_POINTS_START: Tính điểm rating - CustomerId: {CustomerId}, Amount: {Amount:C}",
					customerId, invoiceAmount);

				// ✅ Kiểm tra CustomerId hợp lệ
				if (customerId <= 0)
				{
					_logger.LogWarning("ADD_RATING_POINTS_INVALID_CUSTOMER: CustomerId không hợp lệ - CustomerId: {CustomerId}",
						customerId);
					return false;
				}

				// ✅ Kiểm tra số tiền >= 0
				if (invoiceAmount < 0)
				{
					_logger.LogWarning("ADD_RATING_POINTS_INVALID_AMOUNT: Số tiền không hợp lệ - Amount: {Amount}",
						invoiceAmount);
					return false;
				}

				// ✅ Lấy thông tin khách hàng
				var customer = await _customerRepository.GetById(customerId);
				if (customer == null || customer.DeleteStatus)
				{
					_logger.LogWarning("ADD_RATING_POINTS_CUSTOMER_NOT_FOUND: Khách hàng không tồn tại - CustomerId: {CustomerId}",
						customerId);
					return false;
				}

				// ✅ Tính điểm: 10 điểm / 1,000,000 VNĐ
				int pointsToAdd = (int)(invoiceAmount / 1000000 * 10);

				if (pointsToAdd <= 0)
				{
					_logger.LogInformation("ADD_RATING_POINTS_NO_POINTS: Số tiền quá nhỏ, không cộng điểm - Amount: {Amount}, Points: {Points}",
						invoiceAmount, pointsToAdd);
					return true;
				}

				// ✅ Cộng điểm
				int oldRatingPoints = customer.RatingPoints;
				customer.RatingPoints += pointsToAdd;

				_logger.LogInformation("ADD_RATING_POINTS_CALCULATED: Tính toán điểm - OldPoints: {OldPoints}, PointsToAdd: {PointsToAdd}, NewPoints: {NewPoints}",
					oldRatingPoints, pointsToAdd, customer.RatingPoints);

				// ✅ Cập nhật RankMember dựa vào điểm mới
				string oldRank = customer.RankMember;
				UpdateRankMember(customer);

				_logger.LogInformation("ADD_RATING_POINTS_RANK_UPDATED: Cập nhật rank member - OldRank: {OldRank}, NewRank: {NewRank}, TotalPoints: {TotalPoints}",
					oldRank, customer.RankMember, customer.RatingPoints);

				// ✅ Lưu vào database
				bool result = await _customerRepository.UpdateEntity(customer);

				if (result)
				{
					_logger.LogInformation("ADD_RATING_POINTS_SUCCESS: Cộng điểm thành công - CustomerId: {CustomerId}, OldPoints: {OldPoints}, NewPoints: {NewPoints}, Rank: {Rank}",
						customerId, oldRatingPoints, customer.RatingPoints, customer.RankMember);
				}
				else
				{
					_logger.LogError("ADD_RATING_POINTS_SAVE_FAILED: Lưu điểm thất bại - CustomerId: {CustomerId}",
						customerId);
				}

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ADD_RATING_POINTS_EXCEPTION: Lỗi khi cộng điểm rating - CustomerId: {CustomerId}, Amount: {Amount}",
					customerId, invoiceAmount);
				return false;
			}
		}

		/// <summary>
		/// ✅ Trừ điểm rating khi hoàn tiền thành công
		/// Công thức: 10 điểm / 1,000,000 VNĐ
		/// </summary>
		public async Task<bool> SubtractRatingPointsAsync(int customerId, decimal refundAmount)
		{
			try
			{
				_logger.LogInformation("SUBTRACT_RATING_POINTS_START: Trừ điểm rating - CustomerId: {CustomerId}, Amount: {Amount:C}",
					customerId, refundAmount);

				// ✅ Kiểm tra CustomerId hợp lệ
				if (customerId <= 0)
				{
					_logger.LogWarning("SUBTRACT_RATING_POINTS_INVALID_CUSTOMER: CustomerId không hợp lệ - CustomerId: {CustomerId}",
						customerId);
					return false;
				}

				// ✅ Kiểm tra số tiền >= 0
				if (refundAmount < 0)
				{
					_logger.LogWarning("SUBTRACT_RATING_POINTS_INVALID_AMOUNT: Số tiền không hợp lệ - Amount: {Amount}",
						refundAmount);
					return false;
				}

				// ✅ Lấy thông tin khách hàng
				var customer = await _customerRepository.GetById(customerId);
				if (customer == null || customer.DeleteStatus)
				{
					_logger.LogWarning("SUBTRACT_RATING_POINTS_CUSTOMER_NOT_FOUND: Khách hàng không tồn tại - CustomerId: {CustomerId}",
						customerId);
					return false;
				}

				// ✅ Tính điểm: 10 điểm / 1,000,000 VNĐ
				int pointsToSubtract = (int)(refundAmount / 1000000 * 10);

				if (pointsToSubtract <= 0)
				{
					_logger.LogInformation("SUBTRACT_RATING_POINTS_NO_POINTS: Số tiền quá nhỏ, không trừ điểm - Amount: {Amount}, Points: {Points}",
						refundAmount, pointsToSubtract);
					return true;
				}

				// ✅ Trừ điểm (không được âm)
				int oldRatingPoints = customer.RatingPoints;
				customer.RatingPoints = Math.Max(0, customer.RatingPoints - pointsToSubtract);

				_logger.LogInformation("SUBTRACT_RATING_POINTS_CALCULATED: Tính toán trừ điểm - OldPoints: {OldPoints}, PointsToSubtract: {PointsToSubtract}, NewPoints: {NewPoints}",
					oldRatingPoints, pointsToSubtract, customer.RatingPoints);

				// ✅ Cập nhật RankMember dựa vào điểm mới
				string oldRank = customer.RankMember;
				UpdateRankMember(customer);

				_logger.LogInformation("SUBTRACT_RATING_POINTS_RANK_UPDATED: Cập nhật rank member - OldRank: {OldRank}, NewRank: {NewRank}, TotalPoints: {TotalPoints}",
					oldRank, customer.RankMember, customer.RatingPoints);

				// ✅ Lưu vào database
				bool result = await _customerRepository.UpdateEntity(customer);

				if (result)
				{
					_logger.LogInformation("SUBTRACT_RATING_POINTS_SUCCESS: Trừ điểm thành công - CustomerId: {CustomerId}, OldPoints: {OldPoints}, NewPoints: {NewPoints}, Rank: {Rank}",
						customerId, oldRatingPoints, customer.RatingPoints, customer.RankMember);
				}
				else
				{
					_logger.LogError("SUBTRACT_RATING_POINTS_SAVE_FAILED: Lưu trừ điểm thất bại - CustomerId: {CustomerId}",
						customerId);
				}

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SUBTRACT_RATING_POINTS_EXCEPTION: Lỗi khi trừ điểm rating - CustomerId: {CustomerId}, Amount: {Amount}",
					customerId, refundAmount);
				return false;
			}
		}

		/// <summary>
		/// 🆕 Trừ điểm bán hàng nhân viên khi khách hàng hủy hóa đơn
		/// Công thức: 1 điểm = 40,000 VNĐ (5 điểm = 200,000 VNĐ)
		/// </summary>
		public async Task<bool> SubtractSalesPointsForStaffAsync(int staffId, decimal paidAmount)
		{
			try
			{
				_logger.LogInformation("SUBTRACT_SALES_POINTS_START: Trừ điểm bán hàng nhân viên - StaffId: {StaffId}, PaidAmount: {PaidAmount:C}",
					staffId, paidAmount);

				// ✅ Kiểm tra StaffId hợp lệ
				if (staffId <= 0)
				{
					_logger.LogWarning("SUBTRACT_SALES_POINTS_INVALID_STAFF: StaffId không hợp lệ - StaffId: {StaffId}",
						staffId);
					return false;
				}

				// ✅ Kiểm tra số tiền >= 0
				if (paidAmount < 0)
				{
					_logger.LogWarning("SUBTRACT_SALES_POINTS_INVALID_AMOUNT: Số tiền không hợp lệ - Amount: {Amount}",
						paidAmount);
					return false;
				}

				// ✅ Lấy thông tin nhân viên
				var staff = await _staffRepository.GetById(staffId);
				if (staff == null || staff.DeleteStatus)
				{
					_logger.LogWarning("SUBTRACT_SALES_POINTS_STAFF_NOT_FOUND: Nhân viên không tồn tại - StaffId: {StaffId}",
						staffId);
					return false;
				}

				// ✅ Tính điểm: 1 điểm = 40,000 VNĐ (5 điểm = 200,000 VNĐ)
				int pointsToSubtract = (int)(paidAmount / 40000);

				if (pointsToSubtract <= 0)
				{
					_logger.LogInformation("SUBTRACT_SALES_POINTS_NO_POINTS: Số tiền quá nhỏ, không trừ điểm - Amount: {Amount:C}, Required: 40,000",
						paidAmount);
					return true; // Không lỗi, chỉ không trừ
				}

				// ✅ Trừ điểm (không được âm)
				int oldSalesPoints = staff.SalesPoints ?? 0;
				staff.SalesPoints = Math.Max(0, oldSalesPoints - pointsToSubtract);

				_logger.LogInformation("SUBTRACT_SALES_POINTS_CALCULATED: Tính toán trừ điểm - StaffId: {StaffId}, OldPoints: {OldPoints}, PointsToSubtract: {PointsToSubtract}, NewPoints: {NewPoints}",
					staffId, oldSalesPoints, pointsToSubtract, staff.SalesPoints);

				// ✅ Lưu vào database
				bool result = await _staffRepository.UpdateEntity(staff);

				if (result)
				{
					_logger.LogInformation("SUBTRACT_SALES_POINTS_SUCCESS: Trừ điểm bán hàng thành công - StaffId: {StaffId}, OldPoints: {OldPoints}, NewPoints: {NewPoints}, Amount: {Amount:C}",
						staffId, oldSalesPoints, staff.SalesPoints, paidAmount);
				}
				else
				{
					_logger.LogError("SUBTRACT_SALES_POINTS_SAVE_FAILED: Lưu trừ điểm thất bại - StaffId: {StaffId}",
						staffId);
				}

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SUBTRACT_SALES_POINTS_EXCEPTION: Lỗi khi trừ điểm bán hàng - StaffId: {StaffId}, Amount: {Amount:C}",
					staffId, paidAmount);
				return false;
			}
		}

		/// <summary>
		/// ✅ Cập nhật RankMember dựa vào RatingPoints
		/// Logic: >= 350 = Diamond, >= 200 = Gold, >= 80 = Silver, < 80 = ThanhVien
		/// </summary>
		private void UpdateRankMember(CustomerEntity customer)
		{
			try
			{
				if (customer == null)
				{
					_logger.LogWarning("UPDATE_RANK_MEMBER_NULL_CUSTOMER: Customer là null");
					return;
				}

				string oldRank = customer.RankMember ?? "Bronze";
				string newRank = "Bronze"; 

				if (customer.RatingPoints >= 350)
				{
					newRank = "Diamond";
				}
				else if (customer.RatingPoints >= 200)
				{
					newRank = "Gold";
				}
				else if (customer.RatingPoints >= 80)
				{
					newRank = "Silver";
				}
				else
				{
					newRank = "Bronze";
				}

				customer.RankMember = newRank;

				if (oldRank != newRank)
				{
					_logger.LogInformation("UPDATE_RANK_MEMBER_CHANGED: Thay đổi rank - CustomerId: {CustomerId}, OldRank: {OldRank}, NewRank: {NewRank}, Points: {Points}",
						customer.Id, oldRank, newRank, customer.RatingPoints);
				}
				else
				{
					_logger.LogInformation("UPDATE_RANK_MEMBER_UNCHANGED: Rank không thay đổi - CustomerId: {CustomerId}, Rank: {Rank}, Points: {Points}",
						customer.Id, newRank, customer.RatingPoints);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_RANK_MEMBER_EXCEPTION: Lỗi khi cập nhật rank member - CustomerId: {CustomerId}",
					customer?.Id);
			}
		}
		#endregion
	}
}