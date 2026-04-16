using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.AestheticsInterfaces.ICommonService;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
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
					// ✅ Nếu bị từ chối hoặc thất bại, xóa ngày duyệt
					existingRefund.ApprovedDate = null;
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
		private async Task<string> ProcessVNPayRefund(
			int refundId,
			int invoiceId,
			string originalTransactionId,
			decimal refundAmount,
			string refundReason)
		{
			try
			{
				_logger.LogInformation("PROCESS_VNPAY_REFUND_START: RefundId={RefundId}, Amount={Amount}", refundId, refundAmount);

				string vnp_TmnCode = _configuration.GetSection("Vnpay")["TmnCode"];
				string vnp_HashSecret = _configuration.GetSection("Vnpay")["HashSecret"];
				string vnp_ApiUrl = _configuration.GetSection("Vnpay")["RefundApiUrl"];

				_logger.LogInformation("VNPAY_CONFIG: TmnCode={TmnCode}, HashSecret={SecretLength}chars, ApiUrl={ApiUrl}",
					vnp_TmnCode, vnp_HashSecret?.Length ?? 0, vnp_ApiUrl);

				if (string.IsNullOrEmpty(vnp_TmnCode) || string.IsNullOrEmpty(vnp_HashSecret))
				{
					_logger.LogError("CONFIG_MISSING: TmnCode or HashSecret is empty");
					return null;
				}

				if (refundAmount <= 0)
				{
					_logger.LogError("INVALID_REFUND_AMOUNT: Amount must be > 0 - Amount={Amount}", refundAmount);
					return null;
				}

				// ✅ Lấy invoice để có thời gian thanh toán gốc
				var invoice = await _invoiceRepository.GetById(invoiceId);
				if (invoice == null)
				{
					_logger.LogError("INVOICE_NOT_FOUND: InvoiceId={InvoiceId}", invoiceId);
					return null;
				}

				// ✅ Tạo parameters
				string vnp_RequestId = $"REFUND_{refundId}_{DateTime.UtcNow.Ticks}";
				string vnp_Version = "2.1.0";
				string vnp_Command = "refund";
				string vnp_TransactionType = "02";  
				string vnp_TxnRef = originalTransactionId;
				long vnp_Amount = (long)(refundAmount * 100);

				string sanitizedReason = System.Text.RegularExpressions.Regex.Replace(
					refundReason ?? "Refund",
					@"[^\x20-\x7E]",
					"");
				string vnp_OrderInfo = $"REFUND_{refundId}"; 

				string vnp_CreateBy = "System";  
				
				string vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss");
				
				string vnp_IpAddr = await GetServerIpAddress();

				if (!invoice.PaymentDate.HasValue)
				{
					_logger.LogError("PAYMENT_DATE_MISSING: Invoice không có PaymentDate - InvoiceId={InvoiceId}", invoiceId);
					return null;
				}
				string vnp_TransactionDate = invoice.PaymentDate.Value.ToString("yyyyMMddHHmmss");

				_logger.LogInformation("DEBUG_DATES: PaymentDate_FromDB={PaymentDate}, PaymentDate_Formatted={PaymentDateFormatted}, CreateDate_Formatted={CreateDateFormatted}",
					invoice.PaymentDate.Value,
					vnp_TransactionDate,
					vnp_CreateDate);

				// ✅ BUILD HASH DATA - EXACT ORDER FROM VNPAY DOCS
				string hashData = $"{vnp_RequestId}|{vnp_Version}|{vnp_Command}|{vnp_TmnCode}|{vnp_TransactionType}|{vnp_TxnRef}|{vnp_Amount}||{vnp_TransactionDate}|{vnp_CreateBy}|{vnp_CreateDate}|{vnp_IpAddr}|{vnp_OrderInfo}";

				_logger.LogInformation("HASH_DATA_RAW: {Data}", hashData);

				string vnp_SecureHash = ComputeHmacSHA512(hashData, vnp_HashSecret);
				_logger.LogInformation("HASH_COMPUTED: {Hash}", vnp_SecureHash);
				

				// Thêm vào hàm ProcessVNPayRefund, trước khi tính hash:
				_logger.LogInformation("=== HASH SECRET DEBUG ===");
				_logger.LogInformation("Secret from Config: {Secret}", vnp_HashSecret);
				_logger.LogInformation("Secret Length: {Length}", vnp_HashSecret.Length);
				_logger.LogInformation("Secret Bytes (HEX): {Hex}", 
				string.Join(" ", System.Text.Encoding.UTF8.GetBytes(vnp_HashSecret)
					.Select(b => b.ToString("X2"))));
							_logger.LogInformation("=== END DEBUG ===");

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
				_logger.LogInformation("REQUEST_JSON: {Json}", jsonRequest);

				using (var httpClient = new HttpClient())
				{
					httpClient.Timeout = TimeSpan.FromSeconds(10);
					var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
					httpClient.DefaultRequestHeaders.Add("User-Agent", "Aesthetics-Refund-Service/1.0");

					_logger.LogInformation("CALLING_VNPAY: POST {Url}", vnp_ApiUrl);

					var response = await httpClient.PostAsync(vnp_ApiUrl, content);
					var responseContent = await response.Content.ReadAsStringAsync();

					_logger.LogInformation("VNPAY_RESPONSE: StatusCode={StatusCode}, Body={Body}", response.StatusCode, responseContent);

					if (!response.IsSuccessStatusCode)
					{
						_logger.LogError("VNPAY_HTTP_ERROR: StatusCode={StatusCode}", response.StatusCode);
						return null;
					}

					try
					{
						var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
						var root = jsonDoc.RootElement;

						if (root.TryGetProperty("vnp_ResponseCode", out var responseCode))
						{
							string code = responseCode.GetString();
							_logger.LogInformation("VNPAY_RESPONSE_CODE: {Code}", code);

							if (code == "00")
							{
								_logger.LogInformation("✅ VNPAY_SUCCESS");
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
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ EXCEPTION: RefundId={RefundId}", refundId);
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
	}
}