using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using LinqKit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
	public class InvoiceService : IInvoiceService
	{
		#region Dependencies - Các dependency được inject vào service

		private readonly ILogger<InvoiceService> _logger;
		private readonly IInvoiceRepository _invoiceRepository;
		private readonly IInvoiceDetailsRepository _invoiceDetailsRepository;
		private readonly IProductRepository _productRepository;
		private readonly IServiceRepository _serviceRepository;
		private readonly ITreatmentPlanRepository _treatmentPlanRepository;
		private readonly IVoucherRepository _voucherRepository;
		private readonly ICustomerRepository _customerRepository;
		private readonly IStaffRepository _staffRepository;
		private readonly ICartProductRepository _cartProductRepository;
		private readonly IWalletRepository _walletRepository;

		#endregion

		#region Constructor - Khởi tạo service với dependency injection

		public InvoiceService(
			ILogger<InvoiceService> logger,
			IInvoiceRepository invoiceRepository,
			IInvoiceDetailsRepository invoiceDetailsRepository,
			IProductRepository productRepository,
			IServiceRepository serviceRepository,
			ITreatmentPlanRepository treatmentPlanRepository,
			IVoucherRepository voucherRepository,
			ICustomerRepository customerRepository,
			IStaffRepository staffRepository,
			ICartProductRepository cartProductRepository,
			IWalletRepository walletRepository)
		{
			_logger = logger;
			_invoiceRepository = invoiceRepository;
			_invoiceDetailsRepository = invoiceDetailsRepository;
			_productRepository = productRepository;
			_serviceRepository = serviceRepository;
			_treatmentPlanRepository = treatmentPlanRepository;
			_voucherRepository = voucherRepository;
			_customerRepository = customerRepository;
			_staffRepository = staffRepository;
			_cartProductRepository = cartProductRepository;
			_walletRepository = walletRepository;
		}

		#endregion

		#region Public Methods - Create & Retrieve

		/// <summary>
		/// TẠO HÓA ĐƠN MỚI VỚI NHIỀU SẢN PHẨM (MỖI SẢN PHẨM CÓ SỐ LƯỢNG)
		/// LUỒNG XỬ LÝ:
		/// 1. Validate dữ liệu đầu vào (CustomerId, StaffId, LineItems)
		/// 2. Lặp qua mỗi LineItem (ProductId + Quantity):
		///    - Lấy Product từ database
		///    - Tính giá: Price × Quantity
		///    - Cộng vào tổng giá trị
		/// 3. Áp dụng voucher chung cho hóa đơn (nếu có)
		/// 4. Tính OutstandingBalance, Status dựa trên số tiền thanh toán
		/// 5. Tạo bản ghi Invoice chính
		/// 6. Tạo bản ghi InvoiceDetail cho mỗi sản phẩm (ghi nhận quantity)
		/// 7. Soft delete CartProducts của khách hàng
		/// 8. Mark Voucher as used nếu áp dụng
		/// 9. Log kết quả và trả về true/false
		/// </summary>
		public async Task<bool> create(CreateInvoice invoice)
		{
			try
			{
				_logger.LogInformation("CREATE_INVOICE_START: Bắt đầu tạo hóa đơn - " +
					"KháchhàngID {CustomerId}, LineItemCount: {LineItemCount}",
					invoice?.CustomerId, invoice?.LineItems?.Count ?? 0);

				// BƯỚC 1: Validate dữ liệu đầu vào
				if (invoice == null)
				{
					_logger.LogWarning("CREATE_INVOICE_INVALID: Dữ liệu đầu vào không hợp lệ");
					return false;
				}

				// BƯỚC 2: Lặp qua mỗi LineItem (ProductId + Quantity) và tính giá
				decimal totalBasePrice = 0;
				var invoiceDetailsToCreate = new List<InvoiceDetailEntity>();
				var processedProductIds = new List<int>();
				int itemIndex = 0;

				foreach (var lineItem in invoice.LineItems)
				{
					itemIndex++;
					int quantity = lineItem.Quantity > 0 ? lineItem.Quantity : 1;

					_logger.LogInformation("CREATE_INVOICE_PROCESS_PRODUCT: Xử lý sản phẩm {ItemIndex} - ProductId: {ProductId}, Quantity: {Quantity}",
						itemIndex, lineItem.ProductId, quantity);

					// Lấy sản phẩm từ database
					var product = await _productRepository.GetById(lineItem.ProductId);
					if (product == null || product.DeleteStatus)
					{
						_logger.LogWarning("CREATE_INVOICE_PRODUCT_NOT_FOUND: Sản phẩm không tồn tại - ProductId: {ProductId}", 
							lineItem.ProductId);
						return false;
					}

					// Tính giá: Price × Quantity
					decimal productPrice = product.SellingPrice ?? 0;
					decimal itemTotal = productPrice * quantity;
					totalBasePrice += itemTotal;

					var invoiceDetail = new InvoiceDetailEntity
					{
						ProductId = lineItem.ProductId,
						Price = productPrice,
						Quantity = quantity,
						TotalMoney = itemTotal,
						DiscountValue = 0,
						FinalPrice = itemTotal,
						Type = invoice.Type,
						DeleteStatus = false
					};

					invoiceDetailsToCreate.Add(invoiceDetail);
					processedProductIds.Add(lineItem.ProductId);

					_logger.LogInformation("CREATE_INVOICE_PRODUCT_CALCULATED: Sản phẩm {ItemIndex} - {ProductName}: {Price:C} × {Quantity} = {Total:C}",
						itemIndex, product.ProductName, productPrice, quantity, itemTotal);
				} // ✅ Vòng lặp kết thúc

				// BƯỚC 3: ✅ Áp dụng voucher chung cho toàn bộ hóa đơn (NẾU CÓ)
				decimal invoiceDiscountValue = 0;
				int? appliedVoucherId = null;

				if (invoice.VoucherId.HasValue)
				{	
					// ✅ Kiểm tra Voucher có tồn tại không
					var voucher = await _voucherRepository.GetById(invoice.VoucherId.Value);
					
					if (voucher != null && !voucher.DeleteStatus && voucher.IsActive == true)
					{
						// ✅ Voucher hợp lệ - tính discount
						invoiceDiscountValue = await CalculateVoucherDiscountAsync(invoice.VoucherId.Value, totalBasePrice);
						appliedVoucherId = invoice.VoucherId.Value;
						
						_logger.LogInformation("CREATE_INVOICE_VOUCHER: Áp dụng voucher chung cho hóa đơn - " +
							"VoucherId: {VoucherId}, GiáGốc: {BasePrice:C}, Giảm: {Discount:C}",
							invoice.VoucherId.Value, totalBasePrice, invoiceDiscountValue);
					}
					else
					{
						// ⚠️ Voucher không hợp lệ - log warning nhưng tiếp tục
						_logger.LogWarning("CREATE_INVOICE_VOUCHER_INVALID: Voucher không hợp lệ - " +
							"VoucherId: {VoucherId} (không tồn tại hoặc đã hết hạn)",
							invoice.VoucherId.Value);
					
						// ✅ Không set VoucherId nếu Voucher không hợp lệ
						appliedVoucherId = null;
					}
				}

				// BƯỚC 4: Tính toán các số tiền cuối cùng
				decimal totalMoney = totalBasePrice;
				decimal finalPrice = totalMoney - invoiceDiscountValue;
				decimal paidAmount = invoice.PaidAmount;
				decimal outstandingBalance = finalPrice - paidAmount;

				// ✅ BƯỚC 5: Khai báo status TẠI ĐÂY - trước khi sử dụng
				string status = GetInvoiceStatus(paidAmount, finalPrice);

				// BƯỚC 6: Tạo entity hóa đơn chính
				var invoiceEntity = new InvoiceEntity
				{
					CustomerId = invoice.CustomerId,
					StaffId = invoice.StaffId,
					VoucherId = appliedVoucherId,  // ✅ Chỉ set nếu Voucher hợp lệ
					TotalMoney = totalMoney,
					DiscountValue = invoiceDiscountValue,
					FinalPrice = finalPrice,
					PaidAmount = paidAmount,
					OutstandingBalance = outstandingBalance,
					DateCreated = DateTime.UtcNow,
					Status = status,
					Type = invoice.Type,
					OrderStatus = "DangXuLy",
					PaymentMethod = invoice.PaymentMethod ?? "ThanhToanOnline",
					DeleteStatus = false
				};

				// BƯỚC 7: Lưu hóa đơn vào database
				var invoiceCreated = await _invoiceRepository.CreateEntity(invoiceEntity);
				if (!invoiceCreated)
				{
					_logger.LogError("CREATE_INVOICE_SAVE_FAILED: Lưu hóa đơn vào database thất bại");
					return false;
				}

				_logger.LogInformation("CREATE_INVOICE_SAVED: Hóa đơn đã được lưu - InvoiceID: {InvoiceId}, " +
					"GiáGốc: {TotalMoney:C}, VoucherGiảm: {Discount:C}, GiáSauGiảm: {FinalPrice:C}",
					invoiceEntity.Id, totalMoney, invoiceDiscountValue, finalPrice);

				// BƯỚC 8: Tạo chi tiết hóa đơn cho mỗi sản phẩm
				int successDetailCount = 0;
				foreach (var detail in invoiceDetailsToCreate)
				{
					detail.InvoiceId = invoiceEntity.Id;
					detail.Status = status;              // ✅ Giờ status đã tồn tại
					detail.StatusComment = false;

					// ✅ Phân bổ voucher discount theo tỷ lệ giá (nếu có discount)
					if (invoiceDiscountValue > 0 && totalBasePrice > 0)
					{
						decimal discountRatio = detail.TotalMoney.Value / totalBasePrice;
						detail.DiscountValue = invoiceDiscountValue * discountRatio;
						detail.FinalPrice = detail.TotalMoney - detail.DiscountValue;

						_logger.LogInformation("CREATE_INVOICE_DETAIL_DISCOUNT: Phân bổ discount - " +
							"ProductId: {ProductId}, GiáGốc: {TotalMoney:C}, TỷLệ: {Ratio:P}, " +
							"Discount: {Discount:C}, GiáSauGiảm: {FinalPrice:C}",
							detail.ProductId, detail.TotalMoney, discountRatio, 
							detail.DiscountValue, detail.FinalPrice);
					}
					else
					{
						detail.DiscountValue = 0;
						detail.FinalPrice = detail.TotalMoney;
					}

					var detailCreated = await _invoiceDetailsRepository.CreateEntity(detail);
					if (detailCreated)
					{
						successDetailCount++;
						_logger.LogInformation("CREATE_INVOICE_DETAIL_CREATED: Chi tiết hóa đơn được tạo - " +
							"DetailID: {DetailId}, ProductId: {ProductId}, Quantity: {Quantity}, " +
							"GiáGốc: {TotalMoney:C}, Discount: {Discount:C}, GiáSauGiảm: {FinalPrice:C}",
							detail.Id, detail.ProductId, detail.Quantity, detail.TotalMoney, 
							detail.DiscountValue, detail.FinalPrice);
					}
					else
					{
						_logger.LogWarning("CREATE_INVOICE_DETAIL_FAILED: Tạo chi tiết hóa đơn thất bại");
					}
				}

				_logger.LogInformation("CREATE_INVOICE_SUCCESS: Tạo hóa đơn thành công - InvoiceID: {InvoiceId}, " +
					"GiáGốc: {TotalMoney:C}, VoucherGiảm: {Discount:C}, GiáSauGiảm: {FinalPrice:C}, " +
					"ChiTiếtThànhCông: {SuccessCount}/{TotalCount}, Status: {Status}",
					invoiceEntity.Id, totalMoney, invoiceDiscountValue, finalPrice,
					successDetailCount, invoiceDetailsToCreate.Count, status);

				// BƯỚC 9: Soft delete CHỈ CartProducts được thêm vào hóa đơn
				await SoftDeleteCartProductsByIds(invoice.CustomerId.Value, processedProductIds);

				// BƯỚC 10: ✅ Mark Voucher as used nếu Voucher hợp lệ được áp dụng
				if (appliedVoucherId.HasValue)
				{
					await MarkVoucherAsUsed(invoice.CustomerId.Value, appliedVoucherId.Value);
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CREATE_INVOICE_EXCEPTION: Lỗi ngoại lệ khi tạo hóa đơn");
				return false;
			}
		}

		/// <summary>
		/// ✅ XÓA MỀM CHỈ CÁC CARTPRODUCTS CÓ PRODUCTID ĐƯỢC THÊM VÀO HÓA ĐƠN
		/// Chỉ xóa những sản phẩm trong processedProductIds, không xóa toàn bộ cart
		/// </summary>
		private async Task SoftDeleteCartProductsByIds(int customerId, List<int> productIds)
		{
			try
			{
				if (productIds == null || productIds.Count == 0)
				{
					_logger.LogInformation("SOFT_DELETE_CART_EMPTY: Không có sản phẩm nào để xóa");
					return;
				}

				_logger.LogInformation("SOFT_DELETE_CART_START: Bắt đầu xóa mềm CartProducts cụ thể - CustomerId: {CustomerId}, ProductCount: {ProductCount}",
					customerId, productIds.Count);

				// ✅ Lấy CHỈ CartProducts của khách hàng có ProductId trong danh sách processedProductIds
				var cartProductsToDelete = await _cartProductRepository.FindByPredicate(x =>
					x.Cart.CustomerId == customerId &&
					productIds.Contains(x.ProductId.Value) &&
					!x.DeleteStatus);

				if (!cartProductsToDelete.Any())
				{
					_logger.LogInformation("SOFT_DELETE_CART_NOT_FOUND: Không tìm thấy CartProducts nào để xóa - CustomerId: {CustomerId}, ProductIds: {ProductIds}",
						customerId, string.Join(",", productIds));
					return;
				}

				_logger.LogInformation("SOFT_DELETE_CART_FOUND: Tìm thấy {Count} CartProducts để xóa", cartProductsToDelete.Count());

				// Xóa mềm từng CartProduct
				int successCount = 0;
				foreach (var cartProduct in cartProductsToDelete)
				{
					cartProduct.DeleteStatus = true;
					var updated = await _cartProductRepository.UpdateEntity(cartProduct);
					if (updated)
					{
						successCount++;
						_logger.LogInformation("SOFT_DELETE_CART_ITEM: Xóa mềm CartProduct thành công - " +
							"CartProductId: {CartProductId}, ProductId: {ProductId}, Quantity: {Quantity}",
							cartProduct.Id, cartProduct.ProductId, cartProduct.Quantity);
					}
					else
					{
						_logger.LogWarning("SOFT_DELETE_CART_ITEM_FAILED: Xóa mềm CartProduct thất bại - CartProductId: {CartProductId}",
							cartProduct.Id);
					}
				}

				_logger.LogInformation("SOFT_DELETE_CART_COMPLETE: Xóa mềm CartProducts hoàn tất - Thành công: {SuccessCount}/{TotalCount}, ProductIds: {ProductIds}",
					successCount, cartProductsToDelete.Count(), string.Join(",", productIds));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SOFT_DELETE_CART_EXCEPTION: Lỗi khi xóa mềm CartProducts - CustomerId: {CustomerId}",
					customerId);
			}
		}

		/// <summary>
		/// LẤY CHI TIẾT ĐẦY ĐỦ CỦA MỘT HÓA ĐƠN
		/// </summary>
		/// <summary>
		/// LẤY DANH SÁCH HÓA ĐƠN CÓ PHÂN TRANG VÀ LỌC (trả về Response Model)
		/// </summary>
		public async Task<BaseDataCollection<InvoiceDetailFullResponseModel>> GetInvoiceDetails(GetInvoice filter)
		{
			try
			{
				_logger.LogInformation("GET_INVOICE_DETAILS_START: Lấy danh sách hóa đơn với filter - " +
					"CustomerId: {CustomerId}, StaffId: {StaffId}, Type: {Type}, Status: {Status}, " +
					"StartDate: {StartDate}, EndDate: {EndDate}, PageNo: {PageNo}, PageSize: {PageSize}",
					filter?.CustomerId, filter?.StaffId, filter?.Type, filter?.Status,
					filter?.StartDate?.Date, filter?.EndDate?.Date, filter?.PageNo, filter?.PageSize);

				// ✅ BƯỚC 1: Validate filter
				if (filter == null)
				{
					_logger.LogWarning("GET_INVOICE_DETAILS_INVALID_FILTER: Filter không hợp lệ");
					return new BaseDataCollection<InvoiceDetailFullResponseModel>
					{
						BaseDatas = new List<InvoiceDetailFullResponseModel>(),
						TotalRecordCount = 0,
						PageIndex = 0,
						PageCount = 0
					};
				}

				// ✅ BƯỚC 2: Xây dựng điều kiện lọc cơ bản
				Expression<Func<InvoiceEntity, bool>> predicate = x => !x.DeleteStatus;

				// Lọc theo CustomerId
				if (filter.CustomerId.HasValue)
				{
					var customerId = filter.CustomerId.Value;
					predicate = predicate.And(x => x.CustomerId == customerId);
					_logger.LogInformation("GET_INVOICE_DETAILS_FILTER_CUSTOMER: Lọc theo CustomerId {CustomerId}", customerId);
				}

				// Lọc theo StaffId
				if (filter.StaffId.HasValue)
				{
					var staffId = filter.StaffId.Value;
					predicate = predicate.And(x => x.StaffId == staffId);
					_logger.LogInformation("GET_INVOICE_DETAILS_FILTER_STAFF: Lọc theo StaffId {StaffId}", staffId);
				}

				// Lọc theo Type (DichVu, SanPham, BanHang, etc.)
				if (!string.IsNullOrEmpty(filter.Type) && filter.Type != "null")
				{
					var type = filter.Type;
					predicate = predicate.And(x => x.Type == type);
					_logger.LogInformation("GET_INVOICE_DETAILS_FILTER_TYPE: Lọc theo Type {Type}", type);
				}

				// Lọc theo Status (ChuaThanhToan, ThanhToanMotPhan, DaThanhToan)
				if (!string.IsNullOrEmpty(filter.Status) && filter.Status != "null")
				{
					var status = filter.Status;
					predicate = predicate.And(x => x.Status == status);
					_logger.LogInformation("GET_INVOICE_DETAILS_FILTER_STATUS: Lọc theo Status {Status}", status);
				}

				// Lọc theo StartDate
				if (filter.StartDate.HasValue)
				{
					var startDate = filter.StartDate.Value.Date;
					predicate = predicate.And(x => x.DateCreated.HasValue && x.DateCreated.Value.Date >= startDate);
					_logger.LogInformation("GET_INVOICE_DETAILS_FILTER_START_DATE: Lọc từ ngày {StartDate:yyyy-MM-dd}", startDate);
				}

				// Lọc theo EndDate
				if (filter.EndDate.HasValue)
				{
					var endDate = filter.EndDate.Value.Date;
					predicate = predicate.And(x => x.DateCreated.HasValue && x.DateCreated.Value.Date <= endDate);
					_logger.LogInformation("GET_INVOICE_DETAILS_FILTER_END_DATE: Lọc đến ngày {EndDate:yyyy-MM-dd}", endDate);
				}

				// ✅ BƯỚC 3: Lấy danh sách hóa đơn thỏa mãn điều kiện (với Include related data)
				var allInvoices = await _invoiceRepository.FindByPredicate(predicate);
				
				// ✅ Eager load related entities
				var invoicesWithDetails = allInvoices
					.AsEnumerable()
					.Select(x => new
					{
						Invoice = x,
						// Force load related entities
						Customer = x.Customer,
						Staff = x.Staff,
						Service = x.Service,
						TreatmentPlan = x.TreatmentPlan
					})
					.ToList();

				var totalCount = invoicesWithDetails.Count;

				_logger.LogInformation("GET_INVOICE_DETAILS_INVOICES_FOUND: Tìm thấy {Count} hóa đơn", totalCount);

				if (totalCount == 0)
				{
					_logger.LogWarning("GET_INVOICE_DETAILS_NO_INVOICES: Không tìm thấy hóa đơn nào thỏa mãn điều kiện");
					return new BaseDataCollection<InvoiceDetailFullResponseModel>
					{
						BaseDatas = new List<InvoiceDetailFullResponseModel>(),
						TotalRecordCount = 0,
						PageIndex = filter.PageNo,
						PageCount = 0
					};
				}

				// ✅ BƯỚC 4: Sắp xếp theo ngày tạo (mới nhất trước)
				var sortedInvoices = invoicesWithDetails
					.OrderByDescending(x => x.Invoice.DateCreated ?? DateTime.MinValue)
					.ToList();

				// ✅ BƯỚC 5: Phân trang
				int pageNo = filter.PageNo > 0 ? filter.PageNo : 1;
				int pageSize = filter.PageSize > 0 ? filter.PageSize : 10;
				int pageCount = (int)Math.Ceiling((double)totalCount / pageSize);

				var pagedInvoices = sortedInvoices
					.Skip((pageNo - 1) * pageSize)
					.Take(pageSize)
					.ToList();

				_logger.LogInformation("GET_INVOICE_DETAILS_PAGING: Phân trang - PageNo: {PageNo}, PageSize: {PageSize}, " +
					"TotalCount: {TotalCount}, PageCount: {PageCount}, CurrentPageRecords: {CurrentPageRecords}",
					pageNo, pageSize, totalCount, pageCount, pagedInvoices.Count);

				// ✅ BƯỚC 6: Lấy chi tiết hóa đơn cho mỗi hóa đơn trong trang
				var result = new List<InvoiceDetailFullResponseModel>();

				foreach (var invoiceData in pagedInvoices)
				{
					var invoice = invoiceData.Invoice;
					_logger.LogInformation("GET_INVOICE_DETAILS_PROCESSING: Xử lý hóa đơn - InvoiceId: {InvoiceId}", invoice.Id);

					// Lấy chi tiết hóa đơn
					var details = await _invoiceDetailsRepository.FindByPredicate(x =>
						x.InvoiceId == invoice.Id && !x.DeleteStatus);

					_logger.LogInformation("GET_INVOICE_DETAILS_DETAIL_COUNT: Tìm thấy {Count} chi tiết cho InvoiceId {InvoiceId}",
						details.Count(), invoice.Id);

					// Map sang Response Models
					var invoiceResponse = await MapToInvoiceResponseAsync(invoice);
					var detailResponseTasks = details
						.OrderBy(x => x.Id)
						.Select(x => MapToInvoiceDetailResponseAsync(x))
						.ToList();
					var detailResponses = await Task.WhenAll(detailResponseTasks);

					var invoiceDetailFull = new InvoiceDetailFullResponseModel
					{
						Invoice = invoiceResponse,
						InvoiceDetails = detailResponses.ToList()
					};

					result.Add(invoiceDetailFull);
				}

				// ✅ BƯỚC 7: Trả về kết quả
				var response = new BaseDataCollection<InvoiceDetailFullResponseModel>
				{
					BaseDatas = result,
					TotalRecordCount = totalCount,
					PageIndex = pageNo,
					PageCount = pageCount
				};

				_logger.LogInformation("GET_INVOICE_DETAILS_SUCCESS: Lấy danh sách thành công - " +
					"TotalCount: {TotalCount}, PageNo: {PageNo}, PageSize: {PageSize}, " +
					"CurrentPageRecords: {CurrentPageRecords}",
					totalCount, pageNo, pageSize, result.Count);

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_INVOICE_DETAILS_EXCEPTION: Lỗi khi lấy danh sách hóa đơn");
				return new BaseDataCollection<InvoiceDetailFullResponseModel>
				{
					BaseDatas = new List<InvoiceDetailFullResponseModel>(),
					TotalRecordCount = 0,
					PageIndex = 0,
					PageCount = 0
				};
			}
		}

		/// <summary>
		/// CẬP NHẬT TRẠNG THÁI THANH TOÁN CỦA HÓA ĐƠN VÀ TẤT CẢ CHI TIẾT (Chung trong 1 transaction)
		/// 
		/// LUỒNG XỬ LÍ:
		/// 1. Validate dữ liệu đầu vào
		/// 2. Lấy hóa đơn từ database
		/// 3. Cộng thêm số tiền thanh toán vào PaidAmount hiện tại
		/// 4. Kiểm tra không vượt quá tổng tiền
		/// 5. Tính toán OutstandingBalance
		/// 6. TỰ ĐỘNG TÍNH TOÁN STATUS:
		///    - ChuaThanhToan: PaidAmount = 0
		///    - ThanhToanMotPhan: 0 < PaidAmount < TotalMoney
		///    - DaThanhToan: PaidAmount >= TotalMoney
		/// 7. CẬP NHẬT INVOICE VÀ TẤT CẢ INVOICE DETAILS cùng status
		/// 8. Cập nhật phương thức thanh toán (nếu có)
		/// 9. Lưu tất cả vào database trong 1 transaction
		/// </summary>
		public async Task<bool> UpdatePaymentStatus(UpdateInvoicePaymentStatus request)
		{
			try
			{
				_logger.LogInformation("UPDATE_PAYMENT_START: Cập nhật trạng thái thanh toán (Invoice + Details) - " +
					"HóaĐơnID {InvoiceId}, SốTiền: {Amount:C}",
					request.InvoiceId, request.AdditionalPaymentAmount);

				// BƯỚC 1: Validate dữ liệu đầu vào
				if (request == null || request.InvoiceId <= 0)
				{
					_logger.LogWarning("UPDATE_PAYMENT_INVALID_INPUT: Dữ liệu đầu vào không hợp lệ");
					return false;
				}

				if (request.AdditionalPaymentAmount < 0)
				{
					_logger.LogWarning("UPDATE_PAYMENT_NEGATIVE: Số tiền thanh toán không thể âm: {Amount}", 
						request.AdditionalPaymentAmount);
					return false;
				}

				// BƯỚC 2: Lấy hóa đơn từ database
				var invoice = await _invoiceRepository.GetById(request.InvoiceId);
				if (invoice == null || invoice.DeleteStatus)
				{
					_logger.LogWarning("UPDATE_PAYMENT_NOT_FOUND: Hóa đơn không tồn tại - HóaĐơnID {InvoiceId}", 
						request.InvoiceId);
					return false;
				}

				_logger.LogInformation("UPDATE_PAYMENT_FOUND: Tìm thấy hóa đơn - TổngTiền: {TotalMoney:C}, " +
					"SốTiềnThanhToán: {PaidAmount:C}",
					invoice.TotalMoney ?? 0, invoice.PaidAmount);

				// BƯỚC 3: Kiểm tra số tiền thanh toán không vượt quá tổng tiền
				decimal finalPrice = invoice.FinalPrice ?? 0;
				decimal newPaidAmount = invoice.PaidAmount + request.AdditionalPaymentAmount ?? 0;

				if (newPaidAmount > finalPrice)
				{
					_logger.LogWarning("UPDATE_PAYMENT_EXCEED: Số tiền thanh toán vượt quá tổng tiền - " +
						"SốTiềnThanhToán: {NewPaidAmount:C}, GiáSauGiảm: {FinalPrice:C}",
						newPaidAmount, finalPrice);
					return false;
				}

				// BƯỚC 4: Cập nhật số tiền đã thanh toán
				invoice.PaidAmount = newPaidAmount;

				// BƯỚC 5: Tính toán OutstandingBalance
				decimal outstandingBalance = finalPrice - newPaidAmount;
				invoice.OutstandingBalance = outstandingBalance;

				// BƯỚC 6: Tính toán lại trạng thái thanh toán tự động
				string newStatus = GetInvoiceStatus(newPaidAmount, finalPrice);
				string oldStatus = invoice.Status;

				_logger.LogInformation("UPDATE_PAYMENT_CALCULATE: Tính toán trạng thái - " +
					"TỷLệThanhToán: {PaymentRatio:P}, Status: {OldStatus} → {NewStatus}",
					(finalPrice > 0 ? (newPaidAmount / finalPrice) : 0), oldStatus, newStatus);

				invoice.Status = newStatus;

				// Cập nhật phương thức thanh toán nếu có
				if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
				{
					invoice.PaymentMethod = request.PaymentMethod;
					_logger.LogInformation("UPDATE_PAYMENT_METHOD: Cập nhật phương thức thanh toán: {PaymentMethod}", 
						request.PaymentMethod);
				}

				// BƯỚC 7: Cập nhật Invoice
				var invoiceUpdated = await _invoiceRepository.UpdateEntity(invoice);
				if (!invoiceUpdated)
				{
					_logger.LogError("UPDATE_PAYMENT_FAILED: Cập nhật hóa đơn vào database thất bại - HóaĐơnID {InvoiceId}", 
						request.InvoiceId);
					return false;
				}

				_logger.LogInformation("UPDATE_PAYMENT_INVOICE_SUCCESS: Cập nhật hóa đơn thành công");

				// BƯỚC 8: Lấy tất cả InvoiceDetail và cập nhật status theo Invoice
				var invoiceDetails = await _invoiceDetailsRepository.FindByPredicate(x =>
					x.InvoiceId == request.InvoiceId && !x.DeleteStatus);

				if (invoiceDetails.Any())
				{
					_logger.LogInformation("UPDATE_PAYMENT_DETAILS_COUNT: Tìm thấy {Count} chi tiết hóa đơn cần cập nhật", 
						invoiceDetails.Count());

					int successDetailCount = 0;
					foreach (var detail in invoiceDetails)
					{
						// Cập nhật status cho mỗi detail
						detail.Status = newStatus;
						
						var detailUpdated = await _invoiceDetailsRepository.UpdateEntity(detail);
						if (detailUpdated)
						{
							successDetailCount++;
							_logger.LogInformation("UPDATE_PAYMENT_DETAIL_SUCCESS: Cập nhật chi tiết thành công - " +
								"DetailID: {DetailId}, Status: {Status}", detail.Id, newStatus);
						}
						else
						{
							_logger.LogWarning("UPDATE_PAYMENT_DETAIL_FAILED: Cập nhật chi tiết thất bại - DetailID: {DetailId}", 
								detail.Id);
						}
					}

					_logger.LogInformation("UPDATE_PAYMENT_DETAILS_RESULT: Cập nhật {SuccessCount}/{TotalCount} chi tiết hóa đơn thành công",
						successDetailCount, invoiceDetails.Count());
				}
				else
				{
					_logger.LogWarning("UPDATE_PAYMENT_NO_DETAILS: Không tìm thấy chi tiết hóa đơn nào - HóaĐơnID {InvoiceId}",
						request.InvoiceId);
				}

				// BƯỚC 9: Log kết quả thành công
				_logger.LogInformation("UPDATE_PAYMENT_COMPLETE_SUCCESS: Cập nhật trạng thái hoàn tất - " +
					"HóaĐơnID: {InvoiceId}, SốTiềnThanhToán: {OldPaidAmount:C} → {NewPaidAmount:C}, " +
					"SốTiềnCòNợ: {OutstandingBalance:C}, Status: {NewStatus}",
					request.InvoiceId, (invoice.PaidAmount - request.AdditionalPaymentAmount), newPaidAmount, 
					outstandingBalance, invoice.Status);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_PAYMENT_EXCEPTION: Lỗi ngoại lệ - InvoiceID {InvoiceId}",
					request?.InvoiceId ?? 0);
				return false;
			}
		}

		/// <summary>
		/// CẬP NHẬT TRẠNG THÁI GIAO HÀNG CỦA HÓA ĐƠN
		/// 
		/// LUỒNG XỬ LÝ:
		/// 1. Validate dữ liệu đầu vào
		/// 2. Lấy hóa đơn từ database, kiểm tra tồn tại
		/// 3. Validate trạng thái giao hàng hợp lệ (DangXuLy, DaGiao, DaHuy)
		/// 4. Cập nhật OrderStatus của Invoice
		/// 5. Lưu vào database
		/// 6. Log kết quả
		/// </summary>
		public async Task<bool> UpdateInvoiceOrderStatus(updateinvoiceorderstatus updateinvoiceorderstatus)
		{
			try
			{
				_logger.LogInformation("UPDATE_ORDER_START: Cập nhật trạng thái giao hàng - " +
					"HóaĐơnID {InvoiceId}, OrderStatus: {OrderStatus}",
					updateinvoiceorderstatus.invoiceId, updateinvoiceorderstatus.orderStatus);

				// BƯỚC 1: Validate dữ liệu đầu vào
				if (updateinvoiceorderstatus.invoiceId <= 0)
				{
					_logger.LogWarning("UPDATE_ORDER_INVALID_INPUT: InvoiceId không hợp lệ - InvoiceId: {InvoiceId}", updateinvoiceorderstatus.invoiceId);
					return false;
				}

				if (string.IsNullOrWhiteSpace(updateinvoiceorderstatus.orderStatus))
				{
					_logger.LogWarning("UPDATE_ORDER_EMPTY_STATUS: OrderStatus không được để trống");
					return false;
				}

				// BƯỚC 2: Validate trạng thái giao hàng hợp lệ
				var validOrderStatuses = new[] { "DangXuLy", "DaGiao", "DaHuy" };
				if (!validOrderStatuses.Contains(updateinvoiceorderstatus.orderStatus))
				{
					_logger.LogWarning("UPDATE_ORDER_INVALID: Trạng thái giao hàng không hợp lệ: {OrderStatus}. " +
						"Chỉ chấp nhận: {ValidStatuses}",
						updateinvoiceorderstatus.orderStatus, string.Join(", ", validOrderStatuses));
					return false;
				}

				// BƯỚC 3: Lấy hóa đơn từ database
				var invoice = await _invoiceRepository.GetById(updateinvoiceorderstatus.invoiceId);
				if (invoice == null || invoice.DeleteStatus)
				{
					_logger.LogWarning("UPDATE_ORDER_NOT_FOUND: Hóa đơn không tồn tại - HóaĐơnID {InvoiceId}", updateinvoiceorderstatus.invoiceId);
					return false;
				}

				_logger.LogInformation("UPDATE_ORDER_FOUND: Tìm thấy hóa đơn - OrderStatus cũ: {OldStatus}",
					invoice.OrderStatus);

				// BƯỚC 4: Cập nhật OrderStatus của Invoice
				string oldOrderStatus = invoice.OrderStatus;
				invoice.OrderStatus = updateinvoiceorderstatus.orderStatus;
				var invoiceUpdated = await _invoiceRepository.UpdateEntity(invoice);

				if (!invoiceUpdated)
				{
					_logger.LogError("UPDATE_ORDER_FAILED: Cập nhật hóa đơn thất bại - HóaĐơnID {InvoiceId}", updateinvoiceorderstatus.invoiceId);
					return false;
				}

				// BƯỚC 5: Log kết quả thành công
				_logger.LogInformation("UPDATE_ORDER_SUCCESS: Cập nhật trạng thái giao hàng thành công - " +
					"HóaĐơnID {InvoiceId}, OrderStatus: {OldStatus} → {NewStatus}",
					updateinvoiceorderstatus.invoiceId, oldOrderStatus, updateinvoiceorderstatus.orderStatus);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_ORDER_EXCEPTION: Lỗi ngoại lệ - HóaĐơnID {InvoiceId}", updateinvoiceorderstatus.invoiceId);
				return false;
			}
		}
		#endregion

		#region Private Methods - Mapping

		/// <summary>
		/// Map InvoiceEntity sang InvoiceResponseModel
		/// ✅ Lấy VoucherCode từ database nếu VoucherId có giá trị
		/// </summary>
		private async Task<InvoiceResponseModel> MapToInvoiceResponseAsync(InvoiceEntity entity)
		{
			// ✅ Lấy Customer nếu CustomerId không null
			string? customerName = null;
			string? customerPhone = null;
			if (entity.CustomerId.HasValue)
			{
				try
				{
					var customer = await _customerRepository.GetById(entity.CustomerId.Value);
					customerName = customer?.FullName;
					customerPhone = customer?.Phone;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "MAP_INVOICE_CUSTOMER_ERROR: Không thể lấy thông tin khách hàng - CustomerId: {CustomerId}", 
						entity.CustomerId.Value);
				}
			}

			// ✅ Lấy Staff nếu StaffId không null
			string? staffName = null;
			if (entity.StaffId.HasValue)
			{
				try
				{
					var staff = await _staffRepository.GetById(entity.StaffId.Value);
					staffName = staff?.FullName;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "MAP_INVOICE_STAFF_ERROR: Không thể lấy thông tin nhân viên - StaffId: {StaffId}", 
						entity.StaffId.Value);
				}
			}

			// ✅ Lấy VoucherCode nếu VoucherId không null
			string? voucherCode = null;
			if (entity.VoucherId.HasValue)
			{
				try
				{
					var voucher = await _voucherRepository.GetById(entity.VoucherId.Value);
					voucherCode = voucher?.Code;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "MAP_INVOICE_VOUCHER_ERROR: Không thể lấy mã voucher - VoucherId: {VoucherId}", 
						entity.VoucherId.Value);
				}
			}

			return new InvoiceResponseModel
			{
				Id = entity.Id,
				CustomerId = entity.CustomerId,
				CustomerName = customerName,
				CustomerPhone = customerPhone,
				StaffId = entity.StaffId,
				StaffName = staffName,
				VoucherId = entity.VoucherId,
				VoucherCode = voucherCode,
				TotalMoney = entity.TotalMoney ?? 0,
				DiscountValue = entity.DiscountValue ?? 0,  
				FinalPrice = entity.FinalPrice ?? 0,        
				PaidAmount = entity.PaidAmount ?? 0,
				OutstandingBalance = entity.OutstandingBalance ?? 0,
				Status = entity.Status,
				OrderStatus = entity.OrderStatus,
				PaymentMethod = entity.PaymentMethod,
				DateCreated = entity.DateCreated,
				Type = entity.Type
			};
		}

		/// <summary>
		/// Map InvoiceDetailEntity sang InvoiceDetailResponseModel
		/// ✅ Lấy VoucherCode từ database nếu VoucherId có giá trị
		/// </summary>
		private async Task<InvoiceDetailResponseModel> MapToInvoiceDetailResponseAsync(InvoiceDetailEntity entity)
		{
			// ✅ Lấy Product nếu ProductId không null
			string? productName = null;
			decimal? productPrice = null;
			if (entity.ProductId.HasValue)
			{
				try
				{
					var product = await _productRepository.GetById(entity.ProductId.Value);
					productName = product?.ProductName;
					productPrice = product?.SellingPrice;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "MAP_DETAIL_PRODUCT_ERROR: Không thể lấy thông tin sản phẩm - ProductId: {ProductId}", 
						entity.ProductId.Value);
				}
			}

			// ✅ Lấy Service nếu ServiceId không null
			string? serviceName = null;
			decimal? servicePrice = null;
			if (entity.ServiceId.HasValue)
			{
				try
				{
					var service = await _serviceRepository.GetById(entity.ServiceId.Value);
					serviceName = service?.ServiceName;
					servicePrice = service?.Price;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "MAP_DETAIL_SERVICE_ERROR: Không thể lấy thông tin dịch vụ - ServiceId: {ServiceId}", 
						entity.ServiceId.Value);
				}
			}

			// ✅ Lấy TreatmentPlan nếu TreatmentPlanId không null
			string? treatmentPlanName = null;
			if (entity.TreatmentPlanId.HasValue)
			{
				try
				{
					var treatmentPlan = await _treatmentPlanRepository.GetById(entity.TreatmentPlanId.Value);
					treatmentPlanName = treatmentPlan?.PlanName;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "MAP_DETAIL_PLAN_ERROR: Không thể lấy thông tin gói điều trị - TreatmentPlanId: {TreatmentPlanId}", 
						entity.TreatmentPlanId.Value);
				}
			}

			// ✅ Lấy VoucherCode nếu VoucherId không null
			string? voucherCode = null;
			if (entity.VoucherId.HasValue)
			{
				try
				{
					var voucher = await _voucherRepository.GetById(entity.VoucherId.Value);
					voucherCode = voucher?.Code;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "MAP_DETAIL_VOUCHER_ERROR: Không thể lấy mã voucher - VoucherId: {VoucherId}", 
						entity.VoucherId.Value);
				}
			}

			return new InvoiceDetailResponseModel
			{
				Id = entity.Id,
				InvoiceId = entity.InvoiceId,
				ProductId = entity.ProductId,
				ProductName = productName,
				ProductPrice = productPrice,
				ServiceId = entity.ServiceId,
				ServiceName = serviceName,
				ServicePrice = servicePrice,
				TreatmentPlanId = entity.TreatmentPlanId,
				TreatmentPlanName = treatmentPlanName,
				VoucherId = entity.VoucherId,
				VoucherCode = voucherCode,
				Price = entity.Price,
				Quantity = entity.Quantity ?? 0,
				DiscountValue = entity.DiscountValue ?? 0,
				TotalMoney = entity.TotalMoney ?? 0,
				FinalPrice = entity.FinalPrice ?? 0,
				Status = entity.Status,
				Type = entity.Type,
				StatusComment = entity.StatusComment ?? false
			};
		}

		#endregion

		#region Private Helper Methods

		/// <summary>
		/// TÍNH TOÁN SỐ TIỀN GIẢM GIÁ TỪ VOUCHER
		/// </summary>
		private async Task<decimal> CalculateVoucherDiscountAsync(int voucherId, decimal basePrice)
		{
			try
			{
				var voucher = await _voucherRepository.GetById(voucherId);
				if (voucher == null || voucher.IsActive == false  || voucher.DeleteStatus)
				{
					_logger.LogWarning("VOUCHER_INVALID: Voucher không hợp lệ - VoucherId {VoucherId}", voucherId);
					return 0;
				}

				var now = DateTime.UtcNow;

				if (voucher.StartDate.HasValue && now < voucher.StartDate.Value)
				{
					_logger.LogWarning("VOUCHER_NOT_STARTED: Voucher chưa có hiệu lực - VoucherId {VoucherId}", voucherId);
					return 0;
				}

				if (voucher.EndDate.HasValue && now > voucher.EndDate.Value)
				{
					_logger.LogWarning("VOUCHER_EXPIRED: Voucher đã hết hạn - VoucherId {VoucherId}", voucherId);
					return 0;
				}

				if (voucher.MinimumOrderValue.HasValue && basePrice < voucher.MinimumOrderValue.Value)
				{
					_logger.LogWarning("VOUCHER_MIN_ORDER: Giá trị đơn hàng không đủ - VoucherId {VoucherId}", voucherId);
					return 0;
				}

				decimal discountAmount = 0;

				if (voucher.DiscountValue.HasValue)
				{
					discountAmount = basePrice * (voucher.DiscountValue.Value / 100);

					if (voucher.MaxValue.HasValue && discountAmount > voucher.MaxValue.Value)
					{
						discountAmount = voucher.MaxValue.Value;
					}
				}

				if (discountAmount > basePrice)
				{
					discountAmount = basePrice;
				}

				_logger.LogInformation("VOUCHER_APPLIED: Áp dụng voucher - VoucherId {VoucherId}, Giảm: {Discount:C}",
					voucherId, discountAmount);

				return discountAmount;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "VOUCHER_EXCEPTION: Lỗi khi tính voucher - VoucherId {VoucherId}", voucherId);
				return 0;
			}
		}

		/// <summary>
		/// XÁC ĐỊNH TRẠNG THÁI THANH TOÁN CỦA HÓA ĐƠN
		/// - ChuaThanhToan: PaidAmount = 0
		/// - ThanhToanMotPhan: 0 < PaidAmount < TotalAmount
		/// - DaThanhToan: PaidAmount >= TotalAmount
		/// </summary>
		private string GetInvoiceStatus(decimal paidAmount, decimal totalAmount)
		{
			if (paidAmount >= totalAmount)
				return "DaThanhToan";
			if (paidAmount > 0)
				return "ThanhToanMotPhan";
			return "ChuaThanhToan";
		}

		/// <summary>
		/// XÓA MỀM TẤT CẢ CARTPRODUCTS CỦA KHÁCH HÀNG
		/// Đặt DeleteStatus = true cho tất cả CartProducts chưa bị xóa của khách hàng
		/// </summary>
		private async Task MarkVoucherAsUsed(int customerId, int voucherId)
		{
			try
			{
				_logger.LogInformation("MARK_VOUCHER_START: Bắt đầu đánh dấu Voucher là đã dùng - CustomerId: {CustomerId}, VoucherId: {VoucherId}",
					customerId, voucherId);

				// Lấy Wallet entry của khách hàng cho Voucher này
				var walletEntry = (await _walletRepository.FindByPredicate(x =>
					x.CustomerId == customerId && x.VoucherId == voucherId && !x.DeleteStatus)).FirstOrDefault();

				if (walletEntry == null)
				{
					_logger.LogWarning("MARK_VOUCHER_NOT_FOUND: Không tìm thấy Wallet entry - CustomerId: {CustomerId}, VoucherId: {VoucherId}",
						customerId, voucherId);
					return;
				}

				// Đánh dấu IsUsed = true
				walletEntry.IsUsed = true;
				var updated = await _walletRepository.UpdateEntity(walletEntry);

				if (updated)
				{
					_logger.LogInformation("MARK_VOUCHER_SUCCESS: Đánh dấu Voucher là đã dùng thành công - WalletId: {WalletId}, VoucherId: {VoucherId}",
						walletEntry.Id, voucherId);
				}
				else
				{
					_logger.LogError("MARK_VOUCHER_FAILED: Đánh dấu Voucher là đã dùng thất bại - WalletId: {WalletId}", walletEntry.Id);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "MARK_VOUCHER_EXCEPTION: Lỗi khi đánh dấu Voucher - CustomerId: {CustomerId}, VoucherId: {VoucherId}",
					customerId, voucherId);
			}
		}

		#endregion
	}
}