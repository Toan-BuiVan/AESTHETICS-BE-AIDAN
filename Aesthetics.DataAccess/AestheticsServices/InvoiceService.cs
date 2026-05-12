using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
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
		private readonly IAppointmentRepositoty _appointmentRepository; 
		private readonly ICustomerTreatmentPlansRepository _customerTreatmentPlansRepository;  
		private readonly ICustomerTreatmentSessionsRepository _customerTreatmentSessionsRepository;
		private readonly IAddressInfoRepository _addressInfoRepository;

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
			IWalletRepository walletRepository,
			IAppointmentRepositoty appointmentRepository, 
			ICustomerTreatmentPlansRepository customerTreatmentPlansRepository,  
			ICustomerTreatmentSessionsRepository customerTreatmentSessionsRepository,
			IAddressInfoRepository addressInfoRepository)  
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
			_appointmentRepository = appointmentRepository;  
			_customerTreatmentPlansRepository = customerTreatmentPlansRepository;  
			_customerTreatmentSessionsRepository = customerTreatmentSessionsRepository;
			_addressInfoRepository = addressInfoRepository;
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

				if (invoice == null)
				{
					_logger.LogWarning("CREATE_INVOICE_INVALID: Dữ liệu đầu vào không hợp lệ");
					return false;
				}

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

					var product = await _productRepository.GetById(lineItem.ProductId);
					if (product == null || product.DeleteStatus)
					{
						_logger.LogWarning("CREATE_INVOICE_PRODUCT_NOT_FOUND: Sản phẩm không tồn tại - ProductId: {ProductId}", 
							lineItem.ProductId);
						return false;
					}

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
				} 

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

				string? shipToAddress = null;
				if (invoice.CustomerId.HasValue && invoice.CustomerId.Value > 0)
				{
					shipToAddress = await GetFormattedDefaultAddressAsync(invoice.CustomerId.Value);
				}

				// BƯỚC 6: Tạo entity hóa đơn chính
				var invoiceEntity = new InvoiceEntity
				{
					CustomerId = invoice.CustomerId,
					StaffId = invoice.StaffId,
					VoucherId = appliedVoucherId,  
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
					ShipToAddress = shipToAddress,
					IsDelivered = false,
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
					detail.Status = status;              
					detail.StatusComment = false;

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

		private async Task<string?> GetFormattedDefaultAddressAsync(int customerId)
		{
			try
			{
				_logger.LogInformation("GET_FORMATTED_ADDRESS: Lấy địa chỉ mặc định - CustomerId: {CustomerId}", customerId);

				var addresses = await _addressInfoRepository.FindByPredicate(a =>
					a.CustomerId == customerId &&
					a.IsDefault == true &&
					a.DeleteStatus == false);

				var defaultAddress = addresses.FirstOrDefault();

				if (defaultAddress == null)
				{
					_logger.LogWarning("GET_FORMATTED_ADDRESS_NOT_FOUND: Không tìm thấy địa chỉ mặc định - CustomerId: {CustomerId}", customerId);
					return null;
				}

				var addressParts = new List<string>();

				if (!string.IsNullOrWhiteSpace(defaultAddress.ProvinceName))
					addressParts.Add(defaultAddress.ProvinceName);

				if (!string.IsNullOrWhiteSpace(defaultAddress.DistrictName))
					addressParts.Add(defaultAddress.DistrictName);

				if (!string.IsNullOrWhiteSpace(defaultAddress.WardName))
					addressParts.Add(defaultAddress.WardName);

				if (!string.IsNullOrWhiteSpace(defaultAddress.DetailAddress))
					addressParts.Add(defaultAddress.DetailAddress);

				var formattedAddress = string.Join(";", addressParts);

				_logger.LogInformation("GET_FORMATTED_ADDRESS_SUCCESS: Định dạng địa chỉ thành công - CustomerId: {CustomerId}, Address: {Address}",
					customerId, formattedAddress);

				return formattedAddress;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_FORMATTED_ADDRESS_EXCEPTION: Lỗi khi lấy địa chỉ định dạng - CustomerId: {CustomerId}", customerId);
				return null;
			}
		}

		public async Task<BaseDataCollection<InvoiceDetailFullResponseModel>> GetInvoiceDetails(GetInvoice filter)
		{
			try
			{
				_logger.LogInformation("GET_INVOICE_DETAILS_START: Lấy danh sách hóa đơn với filter - " +
					"CustomerId: {CustomerId}, StaffId: {StaffId}, Type: {Type}, Status: {Status}, OrderStatuses: {OrderStatuses}, " +
					"StartDate: {StartDate}, EndDate: {EndDate}, PageNo: {PageNo}, PageSize: {PageSize}",
					filter?.CustomerId, filter?.StaffId, filter?.Type, filter?.Status,
					string.Join(", ", filter?.OrderStatuses ?? new List<string>()),
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

				// ✅ BƯỚC 2: Xây dựng điều kiện lọc cơ bản (KHÔNG có OrderStatuses)
				Expression<Func<InvoiceEntity, bool>> predicate = x => !x.DeleteStatus;

				// Lọc theo CustomerId
				if (filter.CustomerId.HasValue && filter.CustomerId.Value > 0)
				{
					var customerId = filter.CustomerId.Value;
					predicate = predicate.And(x => x.CustomerId == customerId);
					_logger.LogInformation("GET_INVOICE_DETAILS_FILTER_CUSTOMER: Lọc theo CustomerId {CustomerId}", customerId);
				}

				// Lọc theo StaffId
				if (filter.StaffId.HasValue && filter.StaffId.Value > 0)
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

				_logger.LogInformation("GET_INVOICE_DETAILS_PREDICATE_BUILT: Đã xây dựng predicate (chưa có OrderStatuses)");

				// ✅ BƯỚC 3: Lấy danh sách hóa đơn từ database (sử dụng predicate cơ bản)
				_logger.LogInformation("GET_INVOICE_DETAILS_FETCHING: Bắt đầu fetch dữ liệu từ database");
				var allInvoices = await _invoiceRepository.FindByPredicate(predicate);

				_logger.LogInformation("GET_INVOICE_DETAILS_FETCHED: Fetch hoàn tất - Số bản ghi: {Count}", allInvoices.Count());

				// ✅ BƯỚC 4: LỌC IN-MEMORY theo OrderStatuses (sau khi fetch từ DB)
				if (filter.OrderStatuses != null && filter.OrderStatuses.Count > 0)
				{
					var validOrderStatuses = filter.OrderStatuses
						.Where(os => !string.IsNullOrEmpty(os) && os != "null")
						.ToList();

					if (validOrderStatuses.Count > 0)
					{
						_logger.LogInformation("GET_INVOICE_DETAILS_FILTER_ORDER_STATUSES_DEBUG: Danh sách OrderStatuses cần lọc: {OrderStatuses}, Count: {Count}",
							string.Join(", ", validOrderStatuses), validOrderStatuses.Count);

						allInvoices = allInvoices
							.AsEnumerable()
							.Where(x => x.OrderStatus != null && validOrderStatuses.Contains(x.OrderStatus))
							.ToList();

						_logger.LogInformation("GET_INVOICE_DETAILS_FILTER_ORDER_STATUSES_APPLIED: Sau khi lọc OrderStatuses - Số bản ghi: {Count}",
							allInvoices.Count());
					}
					else
					{
						_logger.LogWarning("GET_INVOICE_DETAILS_FILTER_ORDER_STATUSES_EMPTY: Danh sách OrderStatuses trống sau khi filter");
					}
				}
				else
				{
					_logger.LogInformation("GET_INVOICE_DETAILS_FILTER_ORDER_STATUSES_NULL: OrderStatuses không được truyền vào hoặc rỗng");
				}

				// ✅ Eager load related entities
				var invoicesWithDetails = allInvoices
					.AsEnumerable()
					.Select(x => new
					{
						Invoice = x,
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

				// ✅ BƯỚC 5: Sắp xếp theo ngày tạo (mới nhất trước)
				var sortedInvoices = invoicesWithDetails
					.OrderByDescending(x => x.Invoice.DateCreated ?? DateTime.MinValue)
					.ToList();

				// ✅ BƯỚC 6: Phân trang
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

				// ✅ BƯỚC 7: Lấy chi tiết hóa đơn cho mỗi hóa đơn trong trang
				var result = new List<InvoiceDetailFullResponseModel>();

				foreach (var invoiceData in pagedInvoices)
				{
					var invoice = invoiceData.Invoice;
					_logger.LogInformation("GET_INVOICE_DETAILS_PROCESSING: Xử lý hóa đơn - InvoiceId: {InvoiceId}, OrderStatus: {OrderStatus}",
						invoice.Id, invoice.OrderStatus ?? "NULL");

					// ✅ Lấy chi tiết hóa đơn
					var details = await _invoiceDetailsRepository.FindByPredicate(x =>
						x.InvoiceId == invoice.Id && !x.DeleteStatus);

					_logger.LogInformation("GET_INVOICE_DETAILS_DETAIL_COUNT: Tìm thấy {Count} chi tiết cho InvoiceId {InvoiceId}",
						details.Count(), invoice.Id);

					// ✅ FIX: Map từng detail lần lượt (SEQUENTIAL) thay vì song song (PARALLEL)
					var detailResponses = new List<InvoiceDetailResponseModel>();
					foreach (var detail in details.OrderBy(x => x.Id))
					{
						// ✅ Gọi async method từng cái một, không gọi Task.WhenAll()
						var detailResponse = await MapToInvoiceDetailResponseAsync(detail);
						detailResponses.Add(detailResponse);
					}

					// Map Invoice
					var invoiceResponse = await MapToInvoiceResponseAsync(invoice);

					var invoiceDetailFull = new InvoiceDetailFullResponseModel
					{
						Invoice = invoiceResponse,
						InvoiceDetails = detailResponses
					};

					result.Add(invoiceDetailFull);
				}

				// ✅ BƯỚC 8: Trả về kết quả
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
		/// 🆕 Cập nhật OrderStatus cho hóa đơn (Update thủ công)
		/// Dùng để update trạng thái giao hàng: DangChoXuLy → DangGiao → DaGiao → KhachHuy
		/// </summary>
		public async Task<bool> UpdateInvoiceOrderStatus(updateinvoiceorderstatus request)
		{
			try
			{
				_logger.LogInformation("UPDATE_INVOICE_ORDER_STATUS_START: Cập nhật OrderStatus hóa đơn - InvoiceId: {InvoiceId}, OrderStatus: {OrderStatus}",
					request.invoiceId, request.orderStatus);

				// ✅ STEP 1: Validate OrderStatus
				var validStatuses = new[] { "DangChoXuLy", "DangGiao", "DaGiao", "KhachHuy" };
				if (!validStatuses.Contains(request.orderStatus))
				{
					_logger.LogWarning("UPDATE_INVOICE_ORDER_STATUS_INVALID: OrderStatus không hợp lệ - InvoiceId: {InvoiceId}, OrderStatus: {OrderStatus}",
						request.invoiceId, request.orderStatus);
					return false;
				}

				// ✅ STEP 2: Lấy hóa đơn từ database
				var invoice = await _invoiceRepository.GetById(request.invoiceId);
				if (invoice == null || invoice.DeleteStatus)
				{
					_logger.LogWarning("UPDATE_INVOICE_ORDER_STATUS_NOT_FOUND: Hóa đơn không tồn tại - InvoiceId: {InvoiceId}",
						request.invoiceId);
					return false;
				}

				// ✅ STEP 3: Kiểm tra OrderStatus hiện tại
				string oldOrderStatus = invoice.OrderStatus ?? "N/A";
				if (oldOrderStatus == request.orderStatus)
				{
					_logger.LogInformation("UPDATE_INVOICE_ORDER_STATUS_NO_CHANGE: OrderStatus không thay đổi - InvoiceId: {InvoiceId}, OrderStatus: {OrderStatus}",
						request.invoiceId, request.orderStatus);
					return true; // Không cần update
				}

				// ✅ STEP 4: Cập nhật OrderStatus
				invoice.OrderStatus = request.orderStatus;

				// ✅ STEP 5: Lưu vào database
				var updated = await _invoiceRepository.UpdateEntity(invoice);
				if (!updated)
				{
					_logger.LogError("UPDATE_INVOICE_ORDER_STATUS_FAILED: Cập nhật hóa đơn thất bại - InvoiceId: {InvoiceId}",
						request.invoiceId);
					return false;
				}

				_logger.LogInformation("UPDATE_INVOICE_ORDER_STATUS_SUCCESS: Cập nhật OrderStatus thành công - InvoiceId: {InvoiceId}, OldOrderStatus: {OldOrderStatus}, NewOrderStatus: {NewOrderStatus}",
					request.invoiceId, oldOrderStatus, request.orderStatus);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_INVOICE_ORDER_STATUS_EXCEPTION: Lỗi khi cập nhật OrderStatus - InvoiceId: {InvoiceId}",
					request.invoiceId);
				return false;
			}
		}

		/// <summary>
		/// 🆕 Update Status Invoice với các logic liên kết:
		/// - Nếu invoice status = "KhachHuy" → Update InvoiceDetail status = "KhachHuy"
		///   và cập nhật Appointment, CustomerTreatmentPlan, CustomerTreatmentSession = "KhachHuy"
		/// - Nếu invoice status = "ChuaThanhToan" → Update InvoiceDetail status = "ChuaThanhToan"
		/// </summary>
		public async Task<bool> UpdateInvoiceStatus(int invoiceId, string newStatus)
		{
			try
			{
				_logger.LogInformation("UPDATE_INVOICE_STATUS_START: Cập nhật status hóa đơn - InvoiceId: {InvoiceId}, NewStatus: {NewStatus}",
					invoiceId, newStatus);

				// ✅ STEP 1: Validate newStatus
				var validStatuses = new[] { "ChuaThanhToan", "ThanhToanMotPhan", "DaThanhToan", "KhachHuy" };
				if (!validStatuses.Contains(newStatus))
				{
					_logger.LogWarning("UPDATE_INVOICE_STATUS_INVALID_STATUS: Status không hợp lệ - InvoiceId: {InvoiceId}, Status: {Status}",
						invoiceId, newStatus);
					return false;
				}

				// ✅ STEP 2: Lấy hóa đơn từ database
				var invoice = await _invoiceRepository.GetById(invoiceId);
				if (invoice == null || invoice.DeleteStatus)
				{
					_logger.LogWarning("UPDATE_INVOICE_STATUS_NOT_FOUND: Hóa đơn không tồn tại - InvoiceId: {InvoiceId}",
						invoiceId);
					return false;
				}

				// ✅ STEP 3: Kiểm tra status hiện tại
				string oldStatus = invoice.Status ?? "N/A";
				if (oldStatus == newStatus)
				{
					_logger.LogInformation("UPDATE_INVOICE_STATUS_NO_CHANGE: Status không thay đổi - InvoiceId: {InvoiceId}, Status: {Status}",
						invoiceId, newStatus);
					return true; // Không cần update
				}

				// ✅ STEP 4: Cập nhật status Invoice
				invoice.Status = newStatus;
				var invoiceUpdated = await _invoiceRepository.UpdateEntity(invoice);
				if (!invoiceUpdated)
				{
					_logger.LogError("UPDATE_INVOICE_STATUS_UPDATE_FAILED: Cập nhật hóa đơn thất bại - InvoiceId: {InvoiceId}",
						invoiceId);
					return false;
				}

				_logger.LogInformation("UPDATE_INVOICE_STATUS_UPDATED: Hóa đơn được cập nhật - InvoiceId: {InvoiceId}, OldStatus: {OldStatus}, NewStatus: {NewStatus}",
					invoiceId, oldStatus, newStatus);

				// ✅ STEP 5: Cập nhật InvoiceDetail
				await UpdateInvoiceDetailsStatusByInvoiceId(invoiceId, newStatus);

				// ✅ STEP 6: Nếu status = "KhachHuy" → Update Appointment, CustomerTreatmentPlan, CustomerTreatmentSession
				if (newStatus == "KhachHuy")
				{
					_logger.LogInformation("UPDATE_INVOICE_STATUS_KhachHuy: Invoice bị hủy - cập nhật các entity liên kết - InvoiceId: {InvoiceId}",
						invoiceId);

					int customerId = invoice.CustomerId ?? 0;

					// Cập nhật Appointment
					await UpdateAppointmentsByCustomerId(customerId);

					// 🆕 Cập nhật CHỈ CustomerTreatmentSession có TreatmentSessionId trùng với hóa đơn
					await UpdateCustomerTreatmentSessionsByInvoiceId(invoiceId, customerId);
				}
				// ✅ STEP 7: Nếu status là một trong các trạng thái thanh toán → Cập nhật PaymentStatus của Appointment
				else if (newStatus == "ChuaThanhToan" || newStatus == "ThanhToanMotPhan" || newStatus == "DaThanhToan")
				{
					_logger.LogInformation("UPDATE_INVOICE_STATUS_PAYMENT: Cập nhật PaymentStatus Appointment - InvoiceId: {InvoiceId}, NewStatus: {NewStatus}",
						invoiceId, newStatus);

					int customerId = invoice.CustomerId ?? 0;
					int paymentStatus = GetPaymentStatusFromInvoiceStatus(newStatus);

					// Cập nhật PaymentStatus của Appointment liên kết
					await UpdateAppointmentPaymentStatusByInvoiceId(invoiceId, customerId, paymentStatus);
				}

				_logger.LogInformation("UPDATE_INVOICE_STATUS_SUCCESS: Cập nhật status hóa đơn thành công - InvoiceId: {InvoiceId}, NewStatus: {NewStatus}",
					invoiceId, newStatus);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_INVOICE_STATUS_EXCEPTION: Lỗi khi cập nhật status hóa đơn - InvoiceId: {InvoiceId}",
					invoiceId);
				return false;
			}
		}

		/// <summary>
		/// 🆕 Lấy danh sách OrderStatus có sẵn
		/// </summary>
		public async Task<List<string>> GetAvailableOrderStatuses()
		{
			try
			{
				_logger.LogInformation("GET_AVAILABLE_ORDER_STATUSES: Lấy danh sách OrderStatus");

				var statuses = new List<string>
				{
					"DangChoXuLy",  // Đang chờ xử lý
					"DangXuLy",		// Đang xử lý
					"DangGiao",     // Đang giao
					"DaGiao",       // Đã giao
					"KhachHuy"         // Đã hủy
				};

				_logger.LogInformation("GET_AVAILABLE_ORDER_STATUSES_SUCCESS: Danh sách OrderStatus - Count: {Count}", statuses.Count);

				return await Task.FromResult(statuses);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_AVAILABLE_ORDER_STATUSES_EXCEPTION: Lỗi khi lấy danh sách OrderStatus");
				return new List<string>();
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
				Type = entity.Type,
				IsDelivered = entity.IsDelivered ?? false,
				IsRefund = entity.IsRefund ?? false,
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
		/// 🆕 Lấy PaymentStatus dựa trên Invoice Status
		/// - ChuaThanhToan → PaymentStatus = 0 (chưa thanh toán)
		/// - ThanhToanMotPhan → PaymentStatus = 2 (thanh toán một phần)
		/// - DaThanhToan → PaymentStatus = 1 (đã thanh toán toàn bộ)
		/// </summary>
		private int GetPaymentStatusFromInvoiceStatus(string invoiceStatus)
		{
			try
			{
				_logger.LogInformation("GET_PAYMENT_STATUS_FROM_INVOICE_STATUS: Chuyển đổi Invoice Status sang PaymentStatus - InvoiceStatus: {InvoiceStatus}",
					invoiceStatus);

				int paymentStatus = invoiceStatus switch
				{
					"ChuaThanhToan" => 0,      
					"ThanhToanMotPhan" => 2,   
					"DaThanhToan" => 1,       
					_ => 0                    
				};

				_logger.LogInformation("GET_PAYMENT_STATUS_FROM_INVOICE_STATUS_RESULT: InvoiceStatus: {InvoiceStatus} → PaymentStatus: {PaymentStatus}",
					invoiceStatus, paymentStatus);

				return paymentStatus;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_PAYMENT_STATUS_FROM_INVOICE_STATUS_EXCEPTION: Lỗi khi chuyển đổi status - InvoiceStatus: {InvoiceStatus}",
					invoiceStatus);
				return 0; 
			}
		}

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

		/// <summary>
		/// 🆕 Cập nhật PaymentStatus của Appointment dựa trên Invoice
		/// - Mỗi hóa đơn chỉ có 1 appointment liên kết
		/// </summary>
		/// <summary>
		/// 🆕 Cập nhật PaymentStatus của Appointment dựa trên Invoice
		/// - Dùng AppointmentId trực tiếp từ Invoice
		/// </summary>
		private async Task UpdateAppointmentPaymentStatusByInvoiceId(int invoiceId, int customerId, int paymentStatus)
		{
			try
			{
				if (invoiceId <= 0 || customerId <= 0)
				{
					_logger.LogWarning("UPDATE_APPOINTMENT_PAYMENT_STATUS_INVALID_PARAMS: Tham số không hợp lệ - InvoiceId: {InvoiceId}, CustomerId: {CustomerId}",
						invoiceId, customerId);
					return;
				}

				_logger.LogInformation("UPDATE_APPOINTMENT_PAYMENT_STATUS_START: Cập nhật PaymentStatus Appointment - InvoiceId: {InvoiceId}, PaymentStatus: {PaymentStatus}",
					invoiceId, paymentStatus);

				// ✅ STEP 1: Lấy hóa đơn
				var invoice = await _invoiceRepository.GetById(invoiceId);
				if (invoice == null || invoice.DeleteStatus)
				{
					_logger.LogWarning("UPDATE_APPOINTMENT_PAYMENT_STATUS_INVOICE_NOT_FOUND: Hóa đơn không tồn tại - InvoiceId: {InvoiceId}",
						invoiceId);
					return;
				}

				// ✅ STEP 2: Lấy AppointmentId từ hóa đơn
				if (!invoice.AppointmentId.HasValue || invoice.AppointmentId.Value <= 0)
				{
					_logger.LogInformation("UPDATE_APPOINTMENT_PAYMENT_STATUS_NO_APPOINTMENT: Hóa đơn không liên kết với appointment - InvoiceId: {InvoiceId}",
						invoiceId);
					return;
				}

				// ✅ STEP 3: Lấy appointment từ AppointmentId
				var appointment = await _appointmentRepository.GetById(invoice.AppointmentId.Value);
				if (appointment == null || appointment.DeleteStatus)
				{
					_logger.LogWarning("UPDATE_APPOINTMENT_PAYMENT_STATUS_APPOINTMENT_NOT_FOUND: Appointment không tồn tại - AppointmentId: {AppointmentId}",
						invoice.AppointmentId.Value);
					return;
				}

				// ✅ STEP 4: Cập nhật PaymentStatus
				appointment.PaymentStatus = paymentStatus;
				var updated = await _appointmentRepository.UpdateEntity(appointment);

				if (updated)
				{
					_logger.LogInformation("UPDATE_APPOINTMENT_PAYMENT_STATUS_SUCCESS: Cập nhật PaymentStatus appointment thành công - " +
						"InvoiceId: {InvoiceId}, AppointmentId: {AppointmentId}, PaymentStatus: {PaymentStatus}",
						invoiceId, appointment.Id, paymentStatus);
				}
				else
				{
					_logger.LogWarning("UPDATE_APPOINTMENT_PAYMENT_STATUS_FAILED: Cập nhật PaymentStatus appointment thất bại - " +
						"InvoiceId: {InvoiceId}, AppointmentId: {AppointmentId}",
						invoiceId, appointment.Id);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_APPOINTMENT_PAYMENT_STATUS_EXCEPTION: Lỗi khi cập nhật PaymentStatus appointment - InvoiceId: {InvoiceId}",
					invoiceId);
			}
		}

		/// <summary>
		/// 🆕 Cập nhật status cho tất cả InvoiceDetail của một Invoice
		/// </summary>
		private async Task UpdateInvoiceDetailsStatusByInvoiceId(int invoiceId, string newStatus)
		{
			try
			{
				_logger.LogInformation("UPDATE_INVOICE_DETAILS_STATUS_START: Cập nhật status chi tiết hóa đơn - InvoiceId: {InvoiceId}, NewStatus: {NewStatus}",
					invoiceId, newStatus);

				var details = await _invoiceDetailsRepository.FindByPredicate(x =>
					x.InvoiceId == invoiceId && !x.DeleteStatus);

				if (!details.Any())
				{
					_logger.LogInformation("UPDATE_INVOICE_DETAILS_STATUS_NO_DETAILS: Không có chi tiết hóa đơn - InvoiceId: {InvoiceId}",
						invoiceId);
					return;
				}

				// 🆕 Dùng UpdateRangeEntities để cập nhật tất cả một lần
				var detailsToUpdate = details.ToList();
				foreach (var detail in detailsToUpdate)
				{
					detail.Status = newStatus;
				}

				var updated = await _invoiceDetailsRepository.UpdateRangeEntities(detailsToUpdate);
				if (updated)
				{
					_logger.LogInformation("UPDATE_INVOICE_DETAILS_STATUS_SUCCESS: Cập nhật {Count} chi tiết hóa đơn - InvoiceId: {InvoiceId}",
						detailsToUpdate.Count, invoiceId);
				}
				else
				{
					_logger.LogWarning("UPDATE_INVOICE_DETAILS_STATUS_PARTIAL: Cập nhật không hoàn toàn - InvoiceId: {InvoiceId}",
						invoiceId);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_INVOICE_DETAILS_STATUS_EXCEPTION: Lỗi khi cập nhật status chi tiết - InvoiceId: {InvoiceId}",
					invoiceId);
			}
		}

		/// <summary>
		/// 🆕 Cập nhật status Appointment thành Cancelled nếu invoice bị hủy
		/// </summary>
		private async Task UpdateAppointmentsByCustomerId(int customerId)
		{
			try
			{
				if (customerId <= 0)
				{
					_logger.LogWarning("UPDATE_APPOINTMENTS_INVALID_CUSTOMER: CustomerId không hợp lệ - CustomerId: {CustomerId}",
						customerId);
					return;
				}

				_logger.LogInformation("UPDATE_APPOINTMENTS_START: Cập nhật status Appointment - CustomerId: {CustomerId}", customerId);

				// Tìm appointment liên kết với customer
				var appointments = await _appointmentRepository.FindByPredicate(x =>
					x.CustomerId == customerId &&
					x.Status != 4 &&  // Không update lại nếu đã Cancelled
					!x.DeleteStatus);

				if (!appointments.Any())
				{
					_logger.LogInformation("UPDATE_APPOINTMENTS_NO_APPOINTMENTS: Không tìm thấy appointment - CustomerId: {CustomerId}",
						customerId);
					return;
				}

				// 🆕 Dùng UpdateRangeEntities để cập nhật tất cả một lần
				var appointmentsToUpdate = appointments.ToList();
				foreach (var appointment in appointmentsToUpdate)
				{
					appointment.Status = 4; // AppointmentStatus.Cancelled
				}

				var updated = await _appointmentRepository.UpdateRangeEntities(appointmentsToUpdate);
				if (updated)
				{
					_logger.LogInformation("UPDATE_APPOINTMENTS_SUCCESS: Cập nhật {Count} appointment - CustomerId: {CustomerId}",
						appointmentsToUpdate.Count, customerId);
				}
				else
				{
					_logger.LogWarning("UPDATE_APPOINTMENTS_PARTIAL: Cập nhật không hoàn toàn - CustomerId: {CustomerId}",
						customerId);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_APPOINTMENTS_EXCEPTION: Lỗi khi cập nhật appointment - CustomerId: {CustomerId}",
					customerId);
			}
		}

		/// <summary>
		/// 🆕 Cập nhật status CustomerTreatmentSession thành "KhachHuy" nếu invoice bị hủy
		/// CHỈ hủy những session có TreatmentSessionId trùng với TreatmentSessionId trong hóa đơn
		/// </summary>
		private async Task UpdateCustomerTreatmentSessionsByInvoiceId(int invoiceId, int customerId)
		{
			try
			{
				if (invoiceId <= 0 || customerId <= 0)
				{
					_logger.LogWarning("UPDATE_CUSTOMER_TREATMENT_SESSIONS_INVALID_PARAMS: Tham số không hợp lệ - InvoiceId: {InvoiceId}, CustomerId: {CustomerId}",
						invoiceId, customerId);
					return;
				}

				_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_SESSIONS_START: Cập nhật status CustomerTreatmentSession - InvoiceId: {InvoiceId}, CustomerId: {CustomerId}",
					invoiceId, customerId);

				// ✅ STEP 1: Lấy hóa đơn để tìm TreatmentSessionId
				var invoice = await _invoiceRepository.GetById(invoiceId);
				if (invoice == null || invoice.DeleteStatus)
				{
					_logger.LogWarning("UPDATE_CUSTOMER_TREATMENT_SESSIONS_INVOICE_NOT_FOUND: Hóa đơn không tồn tại - InvoiceId: {InvoiceId}",
						invoiceId);
					return;
				}

				// ✅ STEP 2: Nếu hóa đơn không có TreatmentSessionId → không cần update session
				if (!invoice.TreatmentSessionId.HasValue)
				{
					_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_SESSIONS_NO_TREATMENT_SESSION: Hóa đơn không phải liệu trình - InvoiceId: {InvoiceId}",
						invoiceId);
					return;
				}

				int treatmentSessionId = invoice.TreatmentSessionId.Value;
				_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_SESSIONS_FOUND_SESSION: Tìm session để hủy - TreatmentSessionId: {TreatmentSessionId}",
					treatmentSessionId);

				// ✅ STEP 3: Lấy CustomerTreatmentPlan của khách hàng
				var customerPlans = await _customerTreatmentPlansRepository.FindByPredicate(x =>
					x.CustomerId == customerId &&
					!x.DeleteStatus);

				if (!customerPlans.Any())
				{
					_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_SESSIONS_NO_PLANS: Khách hàng không có plan - CustomerId: {CustomerId}",
						customerId);
					return;
				}

				// ✅ STEP 4: Tìm CustomerTreatmentSession có TreatmentSessionId trùng với hóa đơn
				var allSessionsToUpdate = new List<CustomerTreatmentSessionEntity>();
				var plansToUpdateStatus = new List<CustomerTreatmentPlanEntity>();

				foreach (var plan in customerPlans)
				{
					var matchingSessions = await _customerTreatmentSessionsRepository.FindByPredicate(x =>
						x.CustomerTreatmentPlanId == plan.Id &&
						x.TreatmentSessionId == treatmentSessionId &&  // 🆕 CHỈ lấy session TRÙNG với invoice
						x.Status != "KhachHuy" &&  // Không update lại nếu đã KhachHuy
						!x.DeleteStatus);

					foreach (var session in matchingSessions)
					{
						session.Status = "KhachHuy";
						allSessionsToUpdate.Add(session);
						_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_SESSIONS_MATCHED: Tìm thấy session cần hủy - SessionId: {SessionId}, PlanId: {PlanId}",
							session.Id, plan.Id);
					}

					// 🆕 Nếu plan này có session được update → thêm vào danh sách để update status plan
					if (matchingSessions.Any())
					{
						plansToUpdateStatus.Add(plan);
					}
				}

				// ✅ STEP 5: Cập nhật những session đã tìm thấy
				if (allSessionsToUpdate.Any())
				{
					var updated = await _customerTreatmentSessionsRepository.UpdateRangeEntities(allSessionsToUpdate);
					if (updated)
					{
						_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_SESSIONS_SUCCESS: Cập nhật {Count} session(s) - InvoiceId: {InvoiceId}, TreatmentSessionId: {TreatmentSessionId}",
							allSessionsToUpdate.Count, invoiceId, treatmentSessionId);

						// 🆕 STEP 6: Cập nhật status CustomerTreatmentPlan dựa trên status của child sessions
						await UpdateCustomerTreatmentPlansStatusBySessionsAsync(plansToUpdateStatus);
					}
					else
					{
						_logger.LogWarning("UPDATE_CUSTOMER_TREATMENT_SESSIONS_PARTIAL: Cập nhật không hoàn toàn - InvoiceId: {InvoiceId}",
							invoiceId);
					}
				}
				else
				{
					_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_SESSIONS_NO_MATCHING_SESSIONS: Không tìm thấy session trùng - InvoiceId: {InvoiceId}, TreatmentSessionId: {TreatmentSessionId}",
						invoiceId, treatmentSessionId);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_CUSTOMER_TREATMENT_SESSIONS_EXCEPTION: Lỗi khi cập nhật session - InvoiceId: {InvoiceId}",
					invoiceId);
			}
		}

		/// <summary>
		/// 🆕 Cập nhật status CustomerTreatmentPlan dựa trên status của child CustomerTreatmentSessions
		/// - Nếu TẤT CẢ session = "KhachHuy" → Plan = "KhachHuy"
		/// - Nếu CÓ session = "KhachHuy" + CÓ session ≠ "KhachHuy" → Plan = "PartialCancelled" hoặc giữ nguyên
		/// - Nếu KHÔNG CÓ session = "KhachHuy" → Plan = "Active"
		/// </summary>
		private async Task UpdateCustomerTreatmentPlansStatusBySessionsAsync(List<CustomerTreatmentPlanEntity> plansToCheck)
		{
			try
			{
				if (!plansToCheck.Any())
				{
					_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_PLANS_STATUS_NO_PLANS: Không có plan để kiểm tra");
					return;
				}

				_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_PLANS_STATUS_START: Kiểm tra và cập nhật status plans - Count: {Count}",
					plansToCheck.Count);

				var plansToUpdate = new List<CustomerTreatmentPlanEntity>();

				foreach (var plan in plansToCheck)
				{
					// Lấy tất cả session của plan
					var allSessions = await _customerTreatmentSessionsRepository.FindByPredicate(x =>
						x.CustomerTreatmentPlanId == plan.Id &&
						!x.DeleteStatus);

					if (!allSessions.Any())
					{
						_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_PLANS_STATUS_NO_SESSIONS: Plan không có session - PlanId: {PlanId}",
							plan.Id);
						continue;
					}

					// Đếm số session bị hủy
					var cancelledSessionsCount = allSessions.Count(s => s.Status == "KhachHuy");
					var totalSessionsCount = allSessions.Count();

					_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_PLANS_STATUS_SESSION_COUNT: PlanId: {PlanId}, CancelledSessions: {Cancelled}/{Total}",
						plan.Id, cancelledSessionsCount, totalSessionsCount);

					string newPlanStatus;

					// 🆕 Logic xác định status mới của plan
					if (cancelledSessionsCount == totalSessionsCount)
					{
						// TẤT CẢ session bị hủy → Plan = "KhachHuy"
						newPlanStatus = "KhachHuy";
					}
					else if (cancelledSessionsCount > 0)
					{
						// CÓ session bị hủy nhưng còn session khác → Plan = "ChoDatLich" hoặc giữ nguyên
						newPlanStatus = plan.Status; // Giữ nguyên status hiện tại
						_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_PLANS_STATUS_PARTIAL: Plan bị hủy một phần - PlanId: {PlanId}, Status: {Status}",
							plan.Id, newPlanStatus);
					}
					else
					{
						// KHÔNG CÓ session bị hủy → Plan = "Active" (hoặc giữ nguyên)
						newPlanStatus = plan.Status; // Giữ nguyên status hiện tại
					}

					// Chỉ update nếu status thay đổi
					if (plan.Status != newPlanStatus)
					{
						plan.Status = newPlanStatus;
						plansToUpdate.Add(plan);
						_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_PLANS_STATUS_CHANGED: Plan status thay đổi - PlanId: {PlanId}, OldStatus: {OldStatus}, NewStatus: {NewStatus}",
							plan.Id, plan.Status, newPlanStatus);
					}
				}

				// ✅ Cập nhật những plan có status thay đổi
				if (plansToUpdate.Any())
				{
					var updated = await _customerTreatmentPlansRepository.UpdateRangeEntities(plansToUpdate);
					if (updated)
					{
						_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_PLANS_STATUS_SUCCESS: Cập nhật {Count} plan status",
							plansToUpdate.Count);
					}
					else
					{
						_logger.LogWarning("UPDATE_CUSTOMER_TREATMENT_PLANS_STATUS_PARTIAL: Cập nhật không hoàn toàn");
					}
				}
				else
				{
					_logger.LogInformation("UPDATE_CUSTOMER_TREATMENT_PLANS_STATUS_NO_CHANGES: Không có plan nào cần cập nhật");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UPDATE_CUSTOMER_TREATMENT_PLANS_STATUS_EXCEPTION: Lỗi khi cập nhật status plans");
			}
		}

		/// <summary>
		/// Xuất thông tin hóa đơn và địa chỉ giao hàng cho danh sách hóa đơn
		/// Export invoice details with customer delivery address for a list of invoice IDs
		/// </summary>
		/// <param name="invoiceIds">Danh sách ID hóa đơn cần xuất</param>
		/// <returns>Danh sách thông tin hóa đơn kèm địa chỉ giao hàng</returns>
		/// <summary>
		/// Xuất thông tin hóa đơn và địa chỉ giao hàng cho danh sách hóa đơn
		/// Export invoice details with customer delivery address for a list of invoice IDs
		/// </summary>
		/// <param name="exportInvoice">Request chứa danh sách ID hóa đơn cần xuất</param>
		/// <returns>Danh sách thông tin hóa đơn kèm địa chỉ giao hàng</returns>
		public async Task<List<InvoiceExportModel>> ExportInvoicesByIdListAsync(ExportInvoiceOrder exportInvoice)
		{
			try
			{
				_logger.LogInformation("EXPORT_INVOICE_START: Bắt đầu xuất hóa đơn - InvoiceCount: {Count}", exportInvoice.invoiceIds?.Count ?? 0);

				if (exportInvoice.invoiceIds == null || exportInvoice.invoiceIds.Count == 0)
				{
					_logger.LogWarning("EXPORT_INVOICE_INVALID: Danh sách ID hóa đơn rỗng");
					return new List<InvoiceExportModel>();
				}

				var exportModels = new List<InvoiceExportModel>();

				foreach (var invoiceId in exportInvoice.invoiceIds)
				{
					try
					{
						// Get invoice data
						var invoice = await _invoiceRepository.GetById(invoiceId);
						if (invoice == null)
						{
							_logger.LogWarning("EXPORT_INVOICE_NOT_FOUND: Hóa đơn không tồn tại - InvoiceId: {InvoiceId}", invoiceId);
							continue;
						}

						// Get customer info
						var customer = invoice.CustomerId.HasValue
							? await _customerRepository.GetById(invoice.CustomerId.Value)
							: null;

						if (customer == null)
						{
							_logger.LogWarning("EXPORT_INVOICE_CUSTOMER_NOT_FOUND: Khách hàng không tồn tại - InvoiceId: {InvoiceId}, CustomerId: {CustomerId}",
								invoiceId, invoice.CustomerId);
							continue;
						}

						// Get delivery address
						var deliveryAddress = await _addressInfoRepository.FindByPredicate(
							a => a.CustomerId == customer.Id && a.IsDefault == true);
						var addressInfo = deliveryAddress.FirstOrDefault();

						// Get invoice details
						var invoiceDetails = await _invoiceDetailsRepository.FindByPredicate(
							d => d.InvoiceId == invoiceId && !d.DeleteStatus);

						var exportModel = new InvoiceExportModel
						{
							// Order Information
							InvoiceId = invoice.Id,
							InvoiceCode = $"INV-{invoice.Id:D6}",
							OrderStatus = invoice.OrderStatus,
							Type = invoice.Type,
							DateCreated = invoice.DateCreated,
							PaymentMethod = invoice.PaymentMethod,
							PaymentStatus = invoice.Status,
							TransactionId = invoice.TransactionId,

							// Payment Information
							TotalMoney = invoice.TotalMoney ?? 0,
							DiscountValue = invoice.DiscountValue ?? 0,
							FinalPrice = invoice.FinalPrice ?? 0,
							PaidAmount = invoice.PaidAmount ?? 0,
							OutstandingBalance = invoice.OutstandingBalance ?? 0,

							// Delivery Information
							IsDelivered = invoice.IsDelivered ?? false,
							ShipToAddress = invoice.ShipToAddress,

							// Customer Information
							CustomerId = customer.Id,
							CustomerName = customer.FullName,
							CustomerPhone = customer.Phone,
							CustomerEmail = customer.Email,

							// Delivery Address
							DeliveryProvince = addressInfo?.ProvinceName,
							DeliveryDistrict = addressInfo?.DistrictName,
							DeliveryWard = addressInfo?.WardName,
							DeliveryDetailAddress = addressInfo?.DetailAddress,
							FullDeliveryAddress = addressInfo != null
								? BuildFullAddress(addressInfo)
								: invoice.ShipToAddress,

							// Invoice Details
							InvoiceDetails = invoiceDetails.Select(d => new InvoiceDetailExportModel
							{
								DetailId = d.Id,
								ProductName = d.ProductId.HasValue && d.Product != null
									? d.Product.ProductName
									: (d.ServiceId.HasValue && d.Service != null
										? d.Service.ServiceName
										: null),
								Quantity = d.Quantity ?? 0,
								Price = d.Price ?? 0,
								TotalMoney = d.TotalMoney ?? 0,
								DiscountValue = d.DiscountValue ?? 0,
								FinalPrice = d.FinalPrice ?? 0,
								Type = d.Type
							}).ToList()
						};

						exportModels.Add(exportModel);

						_logger.LogInformation("EXPORT_INVOICE_SUCCESS: Xuất hóa đơn thành công - InvoiceId: {InvoiceId}, CustomerName: {CustomerName}",
							invoiceId, customer.FullName);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "EXPORT_INVOICE_ERROR: Lỗi khi xuất hóa đơn - InvoiceId: {InvoiceId}", invoiceId);
						continue;
					}
				}

				_logger.LogInformation("EXPORT_INVOICE_COMPLETED: Xuất hóa đơn hoàn tất - ExportedCount: {Count}", exportModels.Count);
				return exportModels;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "EXPORT_INVOICE_FATAL: Lỗi toàn cục khi xuất danh sách hóa đơn");
				return new List<InvoiceExportModel>();
			}
		}

		/// <summary>
		/// Hỗ trợ: Xây dựng địa chỉ giao hàng đầy đủ
		/// Helper: Build complete delivery address
		/// </summary>
		private string BuildFullAddress(AddressInfoEntity addressInfo)
		{
			var addressParts = new List<string>();

			if (!string.IsNullOrWhiteSpace(addressInfo.DetailAddress))
				addressParts.Add(addressInfo.DetailAddress);

			if (!string.IsNullOrWhiteSpace(addressInfo.WardName))
				addressParts.Add(addressInfo.WardName);

			if (!string.IsNullOrWhiteSpace(addressInfo.DistrictName))
				addressParts.Add(addressInfo.DistrictName);

			if (!string.IsNullOrWhiteSpace(addressInfo.ProvinceName))
				addressParts.Add(addressInfo.ProvinceName);

			return string.Join(", ", addressParts.Where(p => !string.IsNullOrWhiteSpace(p)));
		}

		#endregion
	}
}