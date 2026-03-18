using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
	/// <summary>
	/// Service xử lý nghiệp vụ liên quan đến hóa đơn (Invoice)
	/// Bao gồm: Tạo hóa đơn, lấy danh sách hóa đơn, tính toán giá và áp dụng voucher
	/// </summary>
	public class InvoiceService : IInvoiceService
	{
		#region Dependencies - Các dependency được inject vào service

		private readonly ILogger<InvoiceService> _logger;
		private readonly IInvoiceRepository _invoiceRepository;                 // Repository xử lý bảng Invoices
		private readonly IInvoiceDetailsRepository _invoiceDetailsRepository;   // Repository xử lý bảng InvoiceDetails
		private readonly IProductRepository _productRepository;                 // Repository xử lý bảng Products
		private readonly IServiceRepository _serviceRepository;                 // Repository xử lý bảng Services
		private readonly ITreatmentPlanRepository _treatmentPlanRepository;     // Repository xử lý bảng TreatmentPlans
		private readonly IVoucherRepository _voucherRepository;                 // Repository xử lý bảng Vouchers

		#endregion

		#region Constructor - Khởi tạo service với dependency injection

		public InvoiceService(
			ILogger<InvoiceService> logger,
			IInvoiceRepository invoiceRepository,
			IInvoiceDetailsRepository invoiceDetailsRepository,
			IProductRepository productRepository,
			IServiceRepository serviceRepository,
			ITreatmentPlanRepository treatmentPlanRepository,
			IVoucherRepository voucherRepository)
		{
			_logger = logger;
			_invoiceRepository = invoiceRepository;
			_invoiceDetailsRepository = invoiceDetailsRepository;
			_productRepository = productRepository;
			_serviceRepository = serviceRepository;
			_treatmentPlanRepository = treatmentPlanRepository;
			_voucherRepository = voucherRepository;
		}

		#endregion

		#region Public Methods - Các phương thức công khai của service

		/// <summary>
		/// TẠO HÓA ĐƠN MỚI
		/// LUỒNG XỬ LÝ:
		/// 1. Validate dữ liệu đầu vào (kiểm tra null, số tiền âm, phải có ít nhất 1 item)
		/// 2. Tính giá cơ bản từ TreatmentPlan/Service/Product (theo thứ tự ưu tiên)
		/// 3. Áp dụng voucher giảm giá (nếu có) - kiểm tra tính hợp lệ và tính số tiền giảm
		/// 4. Tính toán các số tiền cuối cùng (tổng tiền, số tiền nợ, trạng thái thanh toán)
		/// 5. Tạo bản ghi Invoice trong database
		/// 6. Tạo bản ghi InvoiceDetail tương ứng
		/// 7. Ghi log kết quả và trả về true/false
		/// </summary>
		/// <param name="invoice">Thông tin hóa đơn cần tạo</param>
		/// <returns>True nếu tạo thành công, False nếu có lỗi</returns>
		public async Task<bool> create(CreateInvoice invoice)
		{
			try
			{
				_logger.LogInformation("Bắt đầu tạo hóa đơn mới");

				// BƯỚC 1: Validate dữ liệu đầu vào
				// Kiểm tra các điều kiện bắt buộc: không null, số tiền >= 0, phải có ít nhất 1 item
				if (!ValidateCreateInvoiceInput(invoice))
				{
					_logger.LogWarning("Tạo hóa đơn thất bại: Dữ liệu đầu vào không hợp lệ");
					return false;
				}

				// BƯỚC 2: Tính giá cơ bản dựa trên item được chọn
				// Thứ tự ưu tiên: TreatmentPlan (gói liệu trình) > Service (dịch vụ lẻ) > Product (sản phẩm)
				var pricingInfo = await CalculatePricingAsync(invoice);
				if (pricingInfo == null)
				{
					_logger.LogError("Không thể tính giá cho hóa đơn - không tìm thấy thông tin giá hợp lệ");
					return false;
				}

				// BƯỚC 3: Áp dụng voucher giảm giá (nếu có)
				// Kiểm tra tính hợp lệ của voucher và tính số tiền được giảm
				decimal discountValue = 0;
				if (invoice.VoucherId.HasValue)
				{
					discountValue = await CalculateVoucherDiscountAsync(invoice.VoucherId.Value, pricingInfo.Value.BasePrice);
					_logger.LogInformation("Áp dụng voucher ID {VoucherId} với số tiền giảm: {Discount:C}",
						invoice.VoucherId.Value, discountValue);
				}

				// BƯỚC 4: Tính toán các số tiền cuối cùng
				decimal totalMoney = pricingInfo.Value.BasePrice - discountValue;      // Tổng tiền sau giảm giá
				decimal paidAmount = invoice.PaidAmount;                               // Số tiền đã thanh toán
				decimal outstandingBalance = totalMoney - paidAmount;                  // Số tiền còn nợ

				// BƯỚC 5: Xác định trạng thái thanh toán dựa trên số tiền đã trả
				string status = GetInvoiceStatus(paidAmount, totalMoney);

				// BƯỚC 6: Tạo entity hóa đơn với tất cả thông tin đã tính toán
				var invoiceEntity = new InvoiceEntity
				{
					CustomerId = invoice.CustomerId,
					StaffId = invoice.StaffId,
					VoucherId = invoice.VoucherId,
					TreatmentPlanId = invoice.TreatmentPlanId,
					ServiceId = invoice.ServiceId,
					ProductId = invoice.ProductId,
					DiscountValue = discountValue,
					PaidAmount = paidAmount,
					TotalMoney = totalMoney,
					OutstandingBalance = outstandingBalance,
					DateCreated = DateTime.UtcNow,
					Status = status,
					Type = "BanHang",           // Loại hóa đơn: Bán hàng (không phải nhập hàng)
					OrderStatus = status,   // Trạng thái đơn hàng: Đang xử lý
					PaymentMethod = invoice.PaymentMethod,  // Phương thức thanh toán mặc định: Tiền mặt
					DeleteStatus = false        // Chưa bị xóa
				};

				// BƯỚC 7: Lưu hóa đơn vào database
				var invoiceCreated = await _invoiceRepository.CreateEntity(invoiceEntity);
				if (!invoiceCreated)
				{
					_logger.LogError("Lưu hóa đơn vào database thất bại");
					return false;
				}

				// BƯỚC 8: Tạo chi tiết hóa đơn (InvoiceDetail) - lưu thông tin từng item trong hóa đơn
				var invoiceDetail = new InvoiceDetailEntity
				{
					InvoiceId = invoiceEntity.Id,
					ProductId = invoice.ProductId,
					ServiceId = invoice.ServiceId,
					TreatmentPlanId = invoice.TreatmentPlanId,
					VoucherId = invoice.VoucherId,
					DiscountValue = discountValue,
					Price = pricingInfo.Value.BasePrice,    // Giá gốc trước khi giảm
					Quantity = 1,                           // Mặc định số lượng = 1
					TotalMoney = totalMoney,                // Tổng tiền sau giảm giá
					Status = status,                        // Trạng thái thanh toán
					Type = "Ban",                          // Loại: Bán (không phải Nhập)
					StatusComment = false,                  // Chưa có đánh giá
					DeleteStatus = false                    // Chưa bị xóa
				};

				var detailCreated = await _invoiceDetailsRepository.CreateEntity(invoiceDetail);
				if (!detailCreated)
				{
					_logger.LogWarning("Tạo chi tiết hóa đơn thất bại cho HóaĐơnID {InvoiceId}", invoiceEntity.Id);
					// Không return false vì hóa đơn chính đã tạo thành công
				}

				_logger.LogInformation("Tạo hóa đơn thành công: HóaĐơnID {InvoiceId}, TổngTiền {TotalMoney:C}, Item: {ItemName}",
					invoiceEntity.Id, totalMoney, pricingInfo.Value.ItemName);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi ngoại lệ khi tạo hóa đơn");
				return false;
			}
		}

		/// <summary>
		/// LẤY DANH SÁCH HÓA ĐƠN CÓ PHÂN TRANG VÀ LỌC
		/// LUỒNG XỬ LÝ:
		/// 1. Xây dựng điều kiện lọc cơ bản (chỉ lấy hóa đơn chưa bị xóa)
		/// 2. Áp dụng bộ lọc theo CustomerId (nếu có)
		/// 3. Áp dụng bộ lọc theo StaffId (nếu có)
		/// 4. Lấy tất cả bản ghi thỏa mãn điều kiện và đếm tổng số
		/// 5. Áp dụng phân trang và sắp xếp (mới nhất trước)
		/// 6. Tính tổng số trang và trả về kết quả
		/// </summary>
		/// <param name="invoice">Điều kiện lọc và thông tin phân trang</param>
		/// <returns>Danh sách hóa đơn với thông tin phân trang</returns>
		public async Task<BaseDataCollection<InvoiceEntity>> getlist(GetInvoice invoice)
		{
			try
			{
				_logger.LogInformation("Lấy danh sách hóa đơn với bộ lọc");

				// BƯỚC 1: Xây dựng điều kiện lọc cơ bản - chỉ lấy hóa đơn chưa bị xóa
				Expression<Func<InvoiceEntity, bool>> predicate = x => !x.DeleteStatus;

				// BƯỚC 2: Thêm điều kiện lọc theo khách hàng (nếu có)
				if (invoice.CustomerId.HasValue)
				{
					var customerPredicate = predicate;
					predicate = x => customerPredicate.Compile()(x) && x.CustomerId == invoice.CustomerId.Value;
					_logger.LogInformation("Áp dụng bộ lọc theo KháchhàngID: {CustomerId}", invoice.CustomerId.Value);
				}

				// BƯỚC 3: Thêm điều kiện lọc theo nhân viên (nếu có)
				if (invoice.StaffId.HasValue)
				{
					var staffPredicate = predicate;
					predicate = x => staffPredicate.Compile()(x) && x.StaffId == invoice.StaffId.Value;
					_logger.LogInformation("Áp dụng bộ lọc theo NhânviênID: {StaffId}", invoice.StaffId.Value);
				}

				// BƯỚC 4: Lấy tất cả bản ghi thỏa mãn điều kiện và tính tổng số
				var allMatching = await _invoiceRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count();

				// BƯỚC 5: Áp dụng sắp xếp và phân trang
				// Sắp xếp: Ngày tạo mới nhất trước, sau đó theo ID giảm dần
				var pagedData = allMatching
					.OrderByDescending(x => x.DateCreated ?? DateTime.MinValue)    // Ngày tạo mới nhất trước
					.ThenByDescending(x => x.Id)                                   // ID lớn nhất trước (mới nhất)
					.Skip((invoice.PageNo - 1) * invoice.PageSize)                 // Bỏ qua các trang trước
					.Take(invoice.PageSize)                                        // Lấy số lượng theo trang
					.ToList();

				// BƯỚC 6: Tính tổng số trang
				var totalPages = (int)Math.Ceiling((double)totalCount / invoice.PageSize);

				// BƯỚC 7: Tạo kết quả trả về với thông tin phân trang
				var result = new BaseDataCollection<InvoiceEntity>
				{
					BaseDatas = pagedData,              // Danh sách hóa đơn của trang hiện tại
					PageIndex = invoice.PageNo,         // Trang hiện tại
					PageCount = totalPages              // Tổng số trang
				};

				_logger.LogInformation("Lấy danh sách hóa đơn thành công: Tìm thấy {Count} bản ghi (Trang {PageNo}/{TotalPages})",
					totalCount, invoice.PageNo, totalPages);

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi ngoại lệ khi lấy danh sách hóa đơn");
				// Trả về kết quả rỗng thay vì throw exception
				return new BaseDataCollection<InvoiceEntity>
				{
					BaseDatas = new List<InvoiceEntity>(),
					PageIndex = 0,
					PageCount = 0
				};
			}
		}

		#endregion

		#region Private Helper Methods - Các phương thức hỗ trợ nội bộ

		/// <summary>
		/// VALIDATE DỮ LIỆU ĐẦU VÀO KHI TẠO HÓA ĐƠN
		/// Kiểm tra các điều kiện bắt buộc:
		/// - Request không được null
		/// - Số tiền thanh toán không được âm
		/// - Phải có ít nhất 1 trong 3: ProductId, ServiceId, hoặc TreatmentPlanId
		/// </summary>
		/// <param name="invoice">Thông tin hóa đơn cần validate</param>
		/// <returns>True nếu dữ liệu hợp lệ, False nếu có lỗi</returns>
		private bool ValidateCreateInvoiceInput(CreateInvoice invoice)
		{
			// Kiểm tra request không null
			if (invoice == null)
			{
				_logger.LogWarning("Tạo hóa đơn thất bại: Dữ liệu request bị null");
				return false;
			}

			// Kiểm tra số tiền thanh toán không âm
			if (invoice.PaidAmount < 0)
			{
				_logger.LogWarning("Tạo hóa đơn thất bại: Số tiền thanh toán không thể âm. SốTiền: {PaidAmount}", invoice.PaidAmount);
				return false;
			}

			// Phải có ít nhất một item: Product, Service, hoặc TreatmentPlan
			if (!invoice.ProductId.HasValue && !invoice.ServiceId.HasValue && !invoice.TreatmentPlanId.HasValue)
			{
				_logger.LogWarning("Tạo hóa đơn thất bại: Phải chỉ định ít nhất một trong ProductId, ServiceId, hoặc TreatmentPlanId");
				return false;
			}

			return true;
		}

		/// <summary>
		/// TÍNH GIÁ CƠ BẢN CHO HÓA ĐƠN
		/// Thứ tự ưu tiên khi tính giá:
		/// 1. TreatmentPlan (Gói liệu trình) - ưu tiên cao nhất vì thường có giá ưu đãi
		/// 2. Service (Dịch vụ lẻ) - ưu tiên trung bình
		/// 3. Product (Sản phẩm) - ưu tiên thấp nhất
		/// </summary>
		/// <param name="invoice">Thông tin hóa đơn chứa các ID cần tính giá</param>
		/// <returns>Tuple chứa giá cơ bản và tên item, null nếu không tìm thấy giá hợp lệ</returns>
		private async Task<(decimal BasePrice, string ItemName)?> CalculatePricingAsync(CreateInvoice invoice)
		{
			try
			{
				// ƯU TIÊN 1: Gói liệu trình (TreatmentPlan)
				// Thường có giá ưu đãi hơn so với mua dịch vụ lẻ
				if (invoice.TreatmentPlanId.HasValue)
				{
					var treatmentPlan = await _treatmentPlanRepository.GetById(invoice.TreatmentPlanId.Value);
					if (treatmentPlan?.Price.HasValue == true)
					{
						_logger.LogInformation("Sử dụng giá từ gói liệu trình: {PlanName} - {Price:C}",
							treatmentPlan.PlanName, treatmentPlan.Price.Value);
						return (treatmentPlan.Price.Value, treatmentPlan.PlanName ?? "Gói liệu trình");
					}
				}

				// ƯU TIÊN 2: Dịch vụ lẻ (Service)
				// Giá tiêu chuẩn cho từng buổi dịch vụ
				if (invoice.ServiceId.HasValue)
				{
					var service = await _serviceRepository.GetById(invoice.ServiceId.Value);
					if (service?.Price.HasValue == true)
					{
						_logger.LogInformation("Sử dụng giá từ dịch vụ: {ServiceName} - {Price:C}",
							service.ServiceName, service.Price.Value);
						return (service.Price.Value, service.ServiceName ?? "Dịch vụ");
					}
				}

				// ƯU TIÊN 3: Sản phẩm (Product)
				// Giá bán sản phẩm cho khách hàng
				if (invoice.ProductId.HasValue)
				{
					var product = await _productRepository.GetById(invoice.ProductId.Value);
					if (product?.SellingPrice.HasValue == true)
					{
						_logger.LogInformation("Sử dụng giá từ sản phẩm: {ProductName} - {Price:C}",
							product.ProductName, product.SellingPrice.Value);
						return (product.SellingPrice.Value, product.ProductName ?? "Sản phẩm");
					}
				}

				_logger.LogWarning("Không tìm thấy thông tin giá hợp lệ cho bất kỳ item nào trong hóa đơn");
				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi ngoại lệ khi tính giá cho hóa đơn");
				return null;
			}
		}

		/// <summary>
		/// TÍNH TOÁN SỐ TIỀN GIẢM GIÁ TỪ VOUCHER
		/// Các bước kiểm tra voucher:
		/// 1. Voucher có tồn tại và đang hoạt động không
		/// 2. Voucher có trong thời hạn sử dụng không
		/// 3. Giá trị đơn hàng có đạt ngưỡng tối thiểu không
		/// 4. Tính số tiền giảm và áp dụng giới hạn tối đa
		/// </summary>
		/// <param name="voucherId">ID của voucher</param>
		/// <param name="basePrice">Giá gốc trước khi áp dụng voucher</param>
		/// <returns>Số tiền được giảm (VNĐ)</returns>
		private async Task<decimal> CalculateVoucherDiscountAsync(int voucherId, decimal basePrice)
		{
			try
			{
				// BƯỚC 1: Lấy thông tin voucher và kiểm tra tính hợp lệ cơ bản
				var voucher = await _voucherRepository.GetById(voucherId);
				if (voucher == null || !voucher.IsActive || voucher.DeleteStatus)
				{
					_logger.LogWarning("Voucher không hợp lệ: VoucherID {VoucherId} - Không tồn tại, không hoạt động hoặc đã bị xóa", voucherId);
					return 0;
				}

				// BƯỚC 2: Kiểm tra thời hạn sử dụng voucher
				var now = DateTime.UtcNow;

				// Kiểm tra voucher đã có hiệu lực chưa
				if (voucher.StartDate.HasValue && now < voucher.StartDate.Value)
				{
					_logger.LogWarning("Voucher chưa có hiệu lực: VoucherID {VoucherId} - Bắt đầu từ {StartDate}",
						voucherId, voucher.StartDate.Value);
					return 0;
				}

				// Kiểm tra voucher đã hết hạn chưa
				if (voucher.EndDate.HasValue && now > voucher.EndDate.Value)
				{
					_logger.LogWarning("Voucher đã hết hạn: VoucherID {VoucherId} - Hết hạn lúc {EndDate}",
						voucherId, voucher.EndDate.Value);
					return 0;
				}

				// BƯỚC 3: Kiểm tra giá trị đơn hàng tối thiểu
				if (voucher.MinimumOrderValue.HasValue && basePrice < voucher.MinimumOrderValue.Value)
				{
					_logger.LogWarning("Giá trị đơn hàng không đủ điều kiện: {OrderValue:C} < {MinValue:C} (VoucherID: {VoucherId})",
						basePrice, voucher.MinimumOrderValue.Value, voucherId);
					return 0;
				}

				// BƯỚC 4: Tính toán số tiền giảm giá
				decimal discountAmount = 0;

				if (voucher.DiscountValue.HasValue)
				{
					// Tính giảm giá theo phần trăm
					discountAmount = basePrice * (voucher.DiscountValue.Value / 100);

					_logger.LogInformation("Tính giảm giá theo phần trăm: {BasePrice:C} × {Percent}% = {DiscountAmount:C}",
						basePrice, voucher.DiscountValue.Value, discountAmount);

					// Áp dụng giới hạn giảm giá tối đa (nếu có)
					if (voucher.MaxValue.HasValue && discountAmount > voucher.MaxValue.Value)
					{
						_logger.LogInformation("Áp dụng giới hạn giảm giá tối đa: {DiscountAmount:C} → {MaxValue:C}",
							discountAmount, voucher.MaxValue.Value);
						discountAmount = voucher.MaxValue.Value;
					}
				}

				// BƯỚC 5: Đảm bảo số tiền giảm không vượt quá giá gốc
				if (discountAmount > basePrice)
				{
					_logger.LogInformation("Giới hạn số tiền giảm không vượt quá giá gốc: {DiscountAmount:C} → {BasePrice:C}",
						discountAmount, basePrice);
					discountAmount = basePrice;
				}

				_logger.LogInformation("Áp dụng voucher thành công: VoucherID {VoucherId} - Giảm {Discount:C} từ {BasePrice:C}",
					voucherId, discountAmount, basePrice);

				return discountAmount;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi ngoại lệ khi tính toán voucher: VoucherID {VoucherId}", voucherId);
				return 0;
			}
		}

		/// <summary>
		/// XÁC ĐỊNH TRẠNG THÁI THANH TOÁN CỦA HÓA ĐƠN
		/// Dựa trên tỷ lệ số tiền đã thanh toán so với tổng tiền
		/// </summary>
		/// <param name="paidAmount">Số tiền đã thanh toán</param>
		/// <param name="totalAmount">Tổng tiền của hóa đơn</param>
		/// <returns>Trạng thái thanh toán: "DaThanhToan", "ThanhToanMotPhan", hoặc "ChuaThanhToan"</returns>
		private string GetInvoiceStatus(decimal paidAmount, decimal totalAmount)
		{
			if (paidAmount >= totalAmount)
				return "DaThanhToan";        // Đã thanh toán đủ (100%)
			if (paidAmount > 0)
				return "ThanhToanMotPhan";   // Thanh toán một phần (1-99%)
			return "ChuaThanhToan";          // Chưa thanh toán (0%)
		}

		#endregion
	}
}
