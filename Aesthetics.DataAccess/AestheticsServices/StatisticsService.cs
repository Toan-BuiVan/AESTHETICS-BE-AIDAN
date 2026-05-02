using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
	public class StatisticsService : IStatisticsService
	{
		#region Dependencies

		private readonly ILogger<StatisticsService> _logger;
		private readonly IInvoiceRepository _invoiceRepository;
		private readonly IInvoiceDetailsRepository _invoiceDetailsRepository;
		private readonly IWalletRepository _walletRepository;
		private readonly IVoucherRepository _voucherRepository;
		private readonly IProductRepository _productRepository;
		private readonly IServiceRepository _serviceRepository;
		private readonly IStaffRepository _staffRepository;
		private readonly ICommentRepository _commentRepository;
		private readonly IPerformanceLogRepository _performanceLogRepository;
		private readonly IAppointmentRepositoty _appointmentRepository;
		private readonly IAppointmentAssignmentRepository _appointmentAssignmentRepository;
		private readonly ICustomerRepository _customerRepository;

		#endregion

		#region Constructor

		public StatisticsService(
			ILogger<StatisticsService> logger,
			IInvoiceRepository invoiceRepository,
			IInvoiceDetailsRepository invoiceDetailsRepository,
			IWalletRepository walletRepository,
			IVoucherRepository voucherRepository,
			IProductRepository productRepository,
			IServiceRepository serviceRepository,
			IStaffRepository staffRepository,
			ICommentRepository commentRepository,
			IPerformanceLogRepository performanceLogRepository,
			IAppointmentRepositoty appointmentRepository,
			IAppointmentAssignmentRepository appointmentAssignmentRepository,
			ICustomerRepository customerRepository)
		{
			_logger = logger;
			_invoiceRepository = invoiceRepository;
			_invoiceDetailsRepository = invoiceDetailsRepository;
			_walletRepository = walletRepository;
			_voucherRepository = voucherRepository;
			_productRepository = productRepository;
			_serviceRepository = serviceRepository;
			_staffRepository = staffRepository;
			_commentRepository = commentRepository;
			_performanceLogRepository = performanceLogRepository;
			_appointmentRepository = appointmentRepository;
			_appointmentAssignmentRepository = appointmentAssignmentRepository;
			_customerRepository = customerRepository;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Lấy thống kê toàn diện theo khoảng thời gian
		/// </summary>
		public async Task<MonthlyStatisticsResponse> GetMonthlyStatisticsAsync(DateRangeStatisticsRequest request)
		{
			try
			{
				_logger.LogInformation("GET_MONTHLY_STATISTICS_START: Lấy thống kê - StartDate: {StartDate}, EndDate: {EndDate}",
					request.StartDate.Date, request.EndDate.Date);

				var response = new MonthlyStatisticsResponse
				{
					StartDate = request.StartDate,
					EndDate = request.EndDate,
					TopVouchersUsed = await GetTopVouchersUsedAsync(request),
					TopDoctorsWithBestKPI = await GetTopDoctorsByKPIAsync(request),
					TopSellingProducts = await GetTopSellingProductsAsync(request),
					TopPopularServices = await GetTopPopularServicesAsync(request),
					TopDoctorsWithBestRatings = await GetTopDoctorsByRatingAsync(request),
					TopSalesStaff = await GetTopSalesStaffAsync(request),
					Summary = await GetStatisticsSummaryAsync(request)
				};

				_logger.LogInformation("GET_MONTHLY_STATISTICS_SUCCESS: Thống kê hoàn tất");
				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_MONTHLY_STATISTICS_EXCEPTION: Lỗi khi lấy thống kê toàn diện");
				return new MonthlyStatisticsResponse();
			}
		}

		/// <summary>
		/// Lấy voucher sử dụng nhiều nhất từ hóa đơn
		/// </summary>
		public async Task<List<VoucherUsageStatistic>> GetTopVouchersUsedAsync(DateRangeStatisticsRequest request)
		{
			try
			{
				_logger.LogInformation("GET_TOP_VOUCHERS_START: Bắt đầu lấy voucher sử dụng nhiều nhất từ Invoice");

				// ✅ BƯỚC 1: Lấy tất cả hóa đơn trong khoảng thời gian có VoucherId
				var invoices = await _invoiceRepository.FindByPredicate(x =>
					x.DateCreated.HasValue &&
					x.DateCreated.Value >= request.StartDate &&
					x.DateCreated.Value <= request.EndDate &&
					x.VoucherId.HasValue &&
					!x.DeleteStatus);

				if (!invoices.Any())
				{
					_logger.LogInformation("GET_TOP_VOUCHERS_NO_DATA: Không có hóa đơn sử dụng voucher trong khoảng thời gian này");
					return new List<VoucherUsageStatistic>();
				}

				// ✅ BƯỚC 2: Nhóm theo VoucherId và tính toán
				var voucherStats = invoices
					.GroupBy(i => i.VoucherId)
					.Select(g => new
					{
						VoucherId = g.Key,
						UsageCount = g.Count(),
						TotalDiscount = g.Sum(i => i.DiscountValue ?? 0)
					})
					.OrderByDescending(x => x.UsageCount)
					.Take(request.TopCount)
					.ToList();

				_logger.LogInformation("GET_TOP_VOUCHERS_GROUPED: Tìm thấy {Count} voucher được sử dụng", voucherStats.Count);

				// ✅ BƯỚC 3: Lấy thông tin chi tiết voucher từ database
				var result = new List<VoucherUsageStatistic>();

				foreach (var stat in voucherStats)
				{
					try
					{
						var voucher = await _voucherRepository.GetById(stat.VoucherId.Value);
						if (voucher != null && !voucher.DeleteStatus)
						{
							result.Add(new VoucherUsageStatistic
							{
								VoucherId = voucher.Id,
								VoucherCode = voucher.Code,
								UsageCount = stat.UsageCount,
								TotalDiscount = stat.TotalDiscount,
								AverageDiscount = stat.UsageCount > 0 ? stat.TotalDiscount / stat.UsageCount : 0,
								DiscountValue = voucher.DiscountValue ?? 0,
							});

							_logger.LogInformation("GET_TOP_VOUCHERS_ITEM: VoucherCode: {Code}, UsageCount: {Count}, TotalDiscount: {Discount:C}",
								voucher.Code, stat.UsageCount, stat.TotalDiscount);
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "GET_TOP_VOUCHERS_ITEM_ERROR: Lỗi khi xử lý voucher ID {VoucherId}",
							stat.VoucherId);
					}
				}

				_logger.LogInformation("GET_TOP_VOUCHERS_SUCCESS: Lấy {Count} voucher thành công", result.Count);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_TOP_VOUCHERS_EXCEPTION: Lỗi khi lấy top vouchers");
				return new List<VoucherUsageStatistic>();
			}
		}

		/// <summary>
		/// Lấy bác sĩ có KPI tốt nhất (dựa trên commission + bonus) - CẬP NHẬT với hình ảnh
		/// </summary>
		public async Task<List<DoctorKPIStatistic>> GetTopDoctorsByKPIAsync(DateRangeStatisticsRequest request)
		{
			try
			{
				_logger.LogInformation("GET_TOP_DOCTORS_KPI_START: Bắt đầu lấy bác sĩ có KPI tốt nhất");

				// Lấy tất cả staff là bác sĩ
				var doctors = await _staffRepository.FindByPredicate(x =>
					x.IsDoctor == true &&
					!x.DeleteStatus);

				if (!doctors.Any())
				{
					_logger.LogInformation("GET_TOP_DOCTORS_KPI_NO_DOCTORS: Không có bác sĩ nào");
					return new List<DoctorKPIStatistic>();
				}

				var doctorKPIStats = new List<DoctorKPIStatistic>();

				foreach (var doctor in doctors)
				{
					try
					{
						// Lấy performance log của bác sĩ
						var performanceLogs = await _performanceLogRepository.FindByPredicate(x =>
							x.StaffId == doctor.Id &&
							x.LogDate.HasValue &&
							x.LogDate.Value >= request.StartDate &&
							x.LogDate.Value <= request.EndDate &&
							!x.DeleteStatus);

						var totalCommission = performanceLogs.Sum(p => p.Commission);
						var totalBonus = performanceLogs.Sum(p => p.Bonus);

						// Lấy appointment assignment của bác sĩ
						var assignments = await _appointmentAssignmentRepository.FindByPredicate(x =>
							x.StaffId == doctor.Id &&
							x.AssignedDate.HasValue &&
							x.AssignedDate.Value >= request.StartDate &&
							x.AssignedDate.Value <= request.EndDate &&
							!x.DeleteStatus);

						var appointmentCount = assignments.Count();

						// Lấy thông tin doanh thu từ invoice details nếu có liên kết
						var serviceRevenue = assignments
							.Where(a => a.ServiceId.HasValue)
							.GroupBy(a => a.ServiceId)
							.Sum(g =>
							{
								var service = _serviceRepository.GetById(g.Key.Value).Result;
								return service != null ? (service.Price ?? 0) * g.Count() : 0;
							});

						// Lấy đánh giá từ comment
						var comments = await _commentRepository.FindByPredicate(x =>
							!x.DeleteStatus);

						// Tính KPI Score
						var kpiScore = totalCommission + totalBonus;

						doctorKPIStats.Add(new DoctorKPIStatistic
						{
							StaffId = doctor.Id,
							FullName = doctor.FullName,
							Email = doctor.Email,
							Phone = doctor.Phone,
							Specialization = doctor.Specialization,
							StaffImage = doctor.StaffImage,  // ✅ Thêm hình ảnh
							AppointmentCount = appointmentCount,
							TotalCommission = totalCommission,
							TotalBonus = totalBonus,
							TotalServiceRevenue = serviceRevenue,
							KPIScore = kpiScore,
							AverageRating = comments.Any() ? comments.Average(c => c.Rating ?? 0) : 0,
							RatingCount = comments.Count()
						});

						_logger.LogInformation("GET_TOP_DOCTORS_KPI_ITEM: DoctorId: {DoctorId}, FullName: {FullName}, KPIScore: {KPIScore:C}",
							doctor.Id, doctor.FullName, kpiScore);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "GET_TOP_DOCTORS_KPI_ITEM_ERROR: Lỗi khi xử lý bác sĩ ID {DoctorId}",
							doctor.Id);
					}
				}

				var topDoctors = doctorKPIStats
					.OrderByDescending(x => x.KPIScore)
					.Take(request.TopCount)
					.ToList();

				_logger.LogInformation("GET_TOP_DOCTORS_KPI_SUCCESS: Lấy {Count} bác sĩ thành công", topDoctors.Count);
				return topDoctors;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_TOP_DOCTORS_KPI_EXCEPTION: Lỗi khi lấy top doctors by KPI");
				return new List<DoctorKPIStatistic>();
			}
		}

		/// <summary>
		/// Lấy sản phẩm bán chạy nhất
		/// </summary>
		public async Task<List<ProductSalesStatistic>> GetTopSellingProductsAsync(DateRangeStatisticsRequest request)
		{
			try
			{
				_logger.LogInformation("GET_TOP_SELLING_PRODUCTS_START: Bắt đầu lấy sản phẩm bán chạy nhất");

				// Lấy tất cả chi tiết hóa đơn có sản phẩm trong khoảng thời gian
				var invoices = await _invoiceRepository.FindByPredicate(x =>
					x.DateCreated.HasValue &&
					x.DateCreated.Value >= request.StartDate &&
					x.DateCreated.Value <= request.EndDate &&
					!x.DeleteStatus);

				var invoiceIds = invoices.Select(i => i.Id).ToList();

				var invoiceDetails = await _invoiceDetailsRepository.FindByPredicate(x =>
					invoiceIds.Contains(x.InvoiceId.Value) &&
					x.ProductId.HasValue &&
					!x.DeleteStatus);

				if (!invoiceDetails.Any())
				{
					_logger.LogInformation("GET_TOP_SELLING_PRODUCTS_NO_DATA: Không có dữ liệu sản phẩm");
					return new List<ProductSalesStatistic>();
				}

				// Nhóm theo ProductId
				var productStats = invoiceDetails
					.GroupBy(d => d.ProductId)
					.Select(g => new
					{
						ProductId = g.Key,
						QuantitySold = g.Sum(d => d.Quantity ?? 0),
						TotalRevenue = g.Sum(d => d.FinalPrice ?? 0)
					})
					.OrderByDescending(x => x.QuantitySold)
					.Take(request.TopCount)
					.ToList();

				var result = new List<ProductSalesStatistic>();

				foreach (var stat in productStats)
				{
					try
					{
						var product = await _productRepository.GetById(stat.ProductId.Value);
						if (product != null)
						{
							var comments = await _commentRepository.FindByPredicate(x =>
								x.ProductId == product.Id &&
								!x.DeleteStatus);

							var profit = (product.SellingPrice ?? 0) - (product.CostPrice ?? 0);
							var profitMargin = product.SellingPrice > 0
								? (profit / product.SellingPrice ?? 0) * 100
								: 0;

							result.Add(new ProductSalesStatistic
							{
								ProductId = product.Id,
								ProductName = product.ProductName,
								QuantitySold = stat.QuantitySold,
								TotalRevenue = stat.TotalRevenue,
								SellingPrice = product.SellingPrice ?? 0,
								CostPrice = product.CostPrice ?? 0,
								Profit = profit * stat.QuantitySold,
								ProfitMargin = profitMargin,
								RatingCount = comments.Count(),
								AverageRating = comments.Any() ? comments.Average(c => c.Rating ?? 0) : 0
							});
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "GET_TOP_SELLING_PRODUCTS_ITEM_ERROR: Lỗi khi xử lý sản phẩm ID {ProductId}",
							stat.ProductId);
					}
				}

				_logger.LogInformation("GET_TOP_SELLING_PRODUCTS_SUCCESS: Lấy {Count} sản phẩm thành công", result.Count);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_TOP_SELLING_PRODUCTS_EXCEPTION: Lỗi khi lấy top selling products");
				return new List<ProductSalesStatistic>();
			}
		}

		/// <summary>
		/// Lấy dịch vụ có nhiều người dùng nhất
		/// </summary>
		public async Task<List<ServicePopularityStatistic>> GetTopPopularServicesAsync(DateRangeStatisticsRequest request)
		{
			try
			{
				_logger.LogInformation("GET_TOP_POPULAR_SERVICES_START: Bắt đầu lấy dịch vụ phổ biến nhất");

				// Lấy tất cả invoice trong khoảng thời gian
				var invoices = await _invoiceRepository.FindByPredicate(x =>
					x.DateCreated.HasValue &&
					x.DateCreated.Value >= request.StartDate &&
					x.DateCreated.Value <= request.EndDate &&
					!x.DeleteStatus);

				var invoiceIds = invoices.Select(i => i.Id).ToList();

				// Lấy chi tiết hóa đơn có dịch vụ
				var invoiceDetails = await _invoiceDetailsRepository.FindByPredicate(x =>
					invoiceIds.Contains(x.InvoiceId.Value) &&
					x.ServiceId.HasValue &&
					!x.DeleteStatus);

				if (!invoiceDetails.Any())
				{
					_logger.LogInformation("GET_TOP_POPULAR_SERVICES_NO_DATA: Không có dữ liệu dịch vụ");
					return new List<ServicePopularityStatistic>();
				}

				// Nhóm theo ServiceId và đếm khách hàng độc lập
				var serviceStats = invoiceDetails
					.GroupBy(d => d.ServiceId)
					.Select(g => new
					{
						ServiceId = g.Key,
						CustomerCount = g.Select(d => invoices.FirstOrDefault(i => i.Id == d.InvoiceId)?.CustomerId).Distinct().Count(),
						TotalUsage = g.Sum(d => d.Quantity ?? 0),
						TotalRevenue = g.Sum(d => d.FinalPrice ?? 0)
					})
					.OrderByDescending(x => x.CustomerCount)
					.Take(request.TopCount)
					.ToList();

				var result = new List<ServicePopularityStatistic>();

				foreach (var stat in serviceStats)
				{
					try
					{
						var service = await _serviceRepository.GetById(stat.ServiceId.Value);
						if (service != null)
						{
							var comments = await _commentRepository.FindByPredicate(x =>
								x.ServiceId == service.Id &&
								!x.DeleteStatus);

							result.Add(new ServicePopularityStatistic
							{
								ServiceId = service.Id,
								ServiceName = service.ServiceName,
								CustomerCount = stat.CustomerCount,
								TotalUsage = stat.TotalUsage,
								TotalRevenue = stat.TotalRevenue,
								ServicePrice = service.Price ?? 0,
								Duration = service.Duration ?? 0,
								RatingCount = comments.Count(),
								AverageRating = comments.Any() ? comments.Average(c => c.Rating ?? 0) : 0,
								IsCourse = service.IsCourse ?? false
							});
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "GET_TOP_POPULAR_SERVICES_ITEM_ERROR: Lỗi khi xử lý dịch vụ ID {ServiceId}",
							stat.ServiceId);
					}
				}

				_logger.LogInformation("GET_TOP_POPULAR_SERVICES_SUCCESS: Lấy {Count} dịch vụ thành công", result.Count);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_TOP_POPULAR_SERVICES_EXCEPTION: Lỗi khi lấy top popular services");
				return new List<ServicePopularityStatistic>();
			}
		}

		/// <summary>
		/// Lấy bác sĩ có đánh giá tốt nhất (sao cao, nhiều lượt đánh giá) - CẬP NHẬT với hình ảnh
		/// </summary>
		public async Task<List<DoctorRatingStatistic>> GetTopDoctorsByRatingAsync(DateRangeStatisticsRequest request)
		{
			try
			{
				_logger.LogInformation("GET_TOP_DOCTORS_RATING_START: Bắt đầu lấy bác sĩ có đánh giá tốt nhất");

				// Lấy tất cả bác sĩ
				var doctors = await _staffRepository.FindByPredicate(x =>
					x.IsDoctor == true &&
					!x.DeleteStatus);

				if (!doctors.Any())
				{
					_logger.LogInformation("GET_TOP_DOCTORS_RATING_NO_DOCTORS: Không có bác sĩ nào");
					return new List<DoctorRatingStatistic>();
				}

				var doctorRatingStats = new List<DoctorRatingStatistic>();

				foreach (var doctor in doctors)
				{
					try
					{
						// Lấy bình luận/đánh giá của bác sĩ từ appointment assignment
						var assignments = await _appointmentAssignmentRepository.FindByPredicate(x =>
							x.StaffId == doctor.Id &&
							!x.DeleteStatus);

						// Để lấy đánh giá, cần tìm comment liên kết với service của bác sĩ
						var doctorComments = await _commentRepository.FindByPredicate(x =>
							x.CreationDate.HasValue &&
							x.CreationDate.Value >= request.StartDate &&
							x.CreationDate.Value <= request.EndDate &&
							!x.DeleteStatus &&
							(x.ServiceId.HasValue || x.ProductId.HasValue));

						var relevantComments = doctorComments
							.Where(c => assignments.Any(a => a.ServiceId == c.ServiceId))
							.ToList();

						var ratingCount = relevantComments.Count();
						var averageRating = ratingCount > 0 ? relevantComments.Average(c => c.Rating ?? 0) : 0;

						var fiveStarCount = relevantComments.Count(c => c.Rating == 5);
						var fourStarCount = relevantComments.Count(c => c.Rating == 4);
						var threeStarCount = relevantComments.Count(c => c.Rating == 3);
						var twoStarCount = relevantComments.Count(c => c.Rating == 2);
						var oneStarCount = relevantComments.Count(c => c.Rating == 1);

						doctorRatingStats.Add(new DoctorRatingStatistic
						{
							StaffId = doctor.Id,
							FullName = doctor.FullName,
							Email = doctor.Email,
							Specialization = doctor.Specialization,
							StaffImage = doctor.StaffImage,  // ✅ Thêm hình ảnh
							ExperienceYears = doctor.ExperienceYears ?? 0,
							RatingCount = ratingCount,
							AverageRating = averageRating,
							FiveStarCount = fiveStarCount,
							FourStarCount = fourStarCount,
							ThreeStarCount = threeStarCount,
							TwoStarCount = twoStarCount,
							OneStarCount = oneStarCount,
							Degree = doctor.Degree,
							LicenseNumber = doctor.LicenseNumber,
							AppointmentCount = assignments.Count()
						});

						_logger.LogInformation("GET_TOP_DOCTORS_RATING_ITEM: DoctorId: {DoctorId}, FullName: {FullName}, AverageRating: {Rating:F2}",
							doctor.Id, doctor.FullName, averageRating);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "GET_TOP_DOCTORS_RATING_ITEM_ERROR: Lỗi khi xử lý bác sĩ ID {DoctorId}",
							doctor.Id);
					}
				}

				var topDoctors = doctorRatingStats
					.Where(x => x.RatingCount > 0)
					.OrderByDescending(x => x.AverageRating)
					.ThenByDescending(x => x.RatingCount)
					.Take(request.TopCount)
					.ToList();

				_logger.LogInformation("GET_TOP_DOCTORS_RATING_SUCCESS: Lấy {Count} bác sĩ thành công", topDoctors.Count);
				return topDoctors;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_TOP_DOCTORS_RATING_EXCEPTION: Lỗi khi lấy top doctors by rating");
				return new List<DoctorRatingStatistic>();
			}
		}

		/// <summary>
		/// Lấy nhân viên bán hàng tốt nhất
		/// </summary>
		public async Task<List<SalesStaffStatistic>> GetTopSalesStaffAsync(DateRangeStatisticsRequest request)
		{
			try
			{
				_logger.LogInformation("GET_TOP_SALES_STAFF_START: Bắt đầu lấy nhân viên bán hàng tốt nhất");

				// Lấy tất cả staff không phải bác sĩ (nhân viên bán hàng)
				var salesStaff = await _staffRepository.FindByPredicate(x =>
					x.IsDoctor != true &&
					!x.DeleteStatus);

				if (!salesStaff.Any())
				{
					_logger.LogInformation("GET_TOP_SALES_STAFF_NO_STAFF: Không có nhân viên bán hàng nào");
					return new List<SalesStaffStatistic>();
				}

				var salesStaffStats = new List<SalesStaffStatistic>();

				foreach (var staff in salesStaff)
				{
					try
					{
						// Lấy hóa đơn tạo bởi nhân viên
						var invoices = await _invoiceRepository.FindByPredicate(x =>
							x.StaffId == staff.Id &&
							x.DateCreated.HasValue &&
							x.DateCreated.Value >= request.StartDate &&
							x.DateCreated.Value <= request.EndDate &&
							!x.DeleteStatus);

						var invoiceCount = invoices.Count();
						var totalRevenue = invoices.Sum(i => i.FinalPrice ?? 0);

						// Lấy performance log
						var performanceLogs = await _performanceLogRepository.FindByPredicate(x =>
							x.StaffId == staff.Id &&
							x.LogDate.HasValue &&
							x.LogDate.Value >= request.StartDate &&
							x.LogDate.Value <= request.EndDate &&
							!x.DeleteStatus);

						var totalCommission = performanceLogs.Sum(p => p.Commission);
						var totalBonus = performanceLogs.Sum(p => p.Bonus);

						// Đếm sản phẩm và dịch vụ bán
						var invoiceIds = invoices.Select(i => i.Id).ToList();
						var invoiceDetails = await _invoiceDetailsRepository.FindByPredicate(x =>
							invoiceIds.Contains(x.InvoiceId.Value) &&
							!x.DeleteStatus);

						var productsSoldCount = invoiceDetails.Count(d => d.ProductId.HasValue);
						var servicesSoldCount = invoiceDetails.Count(d => d.ServiceId.HasValue);

						salesStaffStats.Add(new SalesStaffStatistic
						{
							StaffId = staff.Id,
							FullName = staff.FullName,
							Email = staff.Email,
							Phone = staff.Phone,
							InvoiceCount = invoiceCount,
							TotalRevenue = totalRevenue,
							TotalCommission = totalCommission,
							TotalBonus = totalBonus,
							SalesPoints = staff.SalesPoints ?? 0,
							ProductsSoldCount = productsSoldCount,
							ServicesSoldCount = servicesSoldCount,
							EmploymentStatus = staff.EmploymentStatus ?? 0
						});
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "GET_TOP_SALES_STAFF_ITEM_ERROR: Lỗi khi xử lý nhân viên ID {StaffId}",
							staff.Id);
					}
				}

				var topSalesStaff = salesStaffStats
					.OrderByDescending(x => x.TotalRevenue)
					.Take(request.TopCount)
					.ToList();

				_logger.LogInformation("GET_TOP_SALES_STAFF_SUCCESS: Lấy {Count} nhân viên thành công", topSalesStaff.Count);
				return topSalesStaff;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_TOP_SALES_STAFF_EXCEPTION: Lỗi khi lấy top sales staff");
				return new List<SalesStaffStatistic>();
			}
		}

		/// <summary>
		/// Lấy thống kê tổng hợp
		/// </summary>
		public async Task<StatisticsSummary> GetStatisticsSummaryAsync(DateRangeStatisticsRequest request)
		{
			try
			{
				_logger.LogInformation("GET_STATISTICS_SUMMARY_START: Bắt đầu lấy thống kê tổng hợp");

				// ✅ BƯỚC 1: Lấy tất cả invoice trong khoảng thời gian
				var invoices = await _invoiceRepository.FindByPredicate(x =>
					x.DateCreated.HasValue &&
					x.DateCreated.Value >= request.StartDate &&
					x.DateCreated.Value <= request.EndDate &&
					!x.DeleteStatus);

				var invoiceIds = invoices.Select(i => i.Id).ToList();

				// ✅ BƯỚC 2: Tính doanh thu
				var totalRevenue = invoices.Sum(i => i.FinalPrice ?? 0);
				var totalInvoices = invoices.Count();
				var totalCustomers = invoices.Select(i => i.CustomerId).Distinct().Count();

				_logger.LogInformation("GET_STATISTICS_SUMMARY_REVENUE: TotalRevenue: {Revenue:C}, TotalInvoices: {Invoices}, TotalCustomers: {Customers}",
					totalRevenue, totalInvoices, totalCustomers);

				// ✅ BƯỚC 3: Thống kê voucher sử dụng từ Invoice
				var invoicesWithVoucher = invoices
					.Where(i => i.VoucherId.HasValue)
					.ToList();

				var totalVouchersUsed = invoicesWithVoucher.Count();
				var totalVoucherDiscount = invoicesWithVoucher.Sum(i => i.DiscountValue ?? 0);

				_logger.LogInformation("GET_STATISTICS_SUMMARY_VOUCHER: TotalVouchersUsed: {Count}, TotalVoucherDiscount: {Discount:C}",
					totalVouchersUsed, totalVoucherDiscount);

				// ✅ BƯỚC 4: Thống kê sản phẩm và dịch vụ
				var invoiceDetails = new List<InvoiceDetailEntity>();
				if (invoiceIds.Any())
				{
					invoiceDetails = (await _invoiceDetailsRepository.FindByPredicate(x =>
						invoiceIds.Contains(x.InvoiceId.Value) &&
						!x.DeleteStatus)).ToList();
				}

				var totalProductsSold = invoiceDetails
					.Where(d => d.ProductId.HasValue)
					.Sum(d => d.Quantity ?? 0);

				var totalServicesProvided = invoiceDetails
					.Where(d => d.ServiceId.HasValue)
					.Count();

				_logger.LogInformation("GET_STATISTICS_SUMMARY_PRODUCTS_SERVICES: TotalProductsSold: {Products}, TotalServicesProvided: {Services}",
					totalProductsSold, totalServicesProvided);

				// ✅ BƯỚC 5: Thống kê bình luận
				var allComments = await _commentRepository.FindByPredicate(x =>
					x.CreationDate.HasValue &&
					x.CreationDate.Value >= request.StartDate &&
					x.CreationDate.Value <= request.EndDate &&
					!x.DeleteStatus);

				var totalComments = allComments.Count();
				var averageRating = totalComments > 0 ? allComments.Average(c => c.Rating ?? 0) : 0;

				_logger.LogInformation("GET_STATISTICS_SUMMARY_COMMENTS: TotalComments: {Count}, AverageRating: {Rating:F2}",
					totalComments, averageRating);

				// ✅ BƯỚC 6: Tạo response
				var summary = new StatisticsSummary
				{
					TotalRevenue = totalRevenue,
					TotalInvoices = totalInvoices,
					TotalCustomers = totalCustomers,
					TotalVouchersUsed = totalVouchersUsed,
					TotalVoucherDiscount = totalVoucherDiscount,
					TotalProductsSold = totalProductsSold,
					TotalServicesProvided = totalServicesProvided,
					TotalComments = totalComments,
					AverageRating = averageRating
				};

				_logger.LogInformation("GET_STATISTICS_SUMMARY_SUCCESS: Thống kê tổng hợp hoàn tất - TotalRevenue: {Revenue:C}, TotalInvoices: {Invoices}",
					totalRevenue, totalInvoices);

				return summary;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_STATISTICS_SUMMARY_EXCEPTION: Lỗi khi lấy thống kê tổng hợp");
				return new StatisticsSummary();
			}
		}

		/// <summary>
		/// 🆕 Lấy thống kê thanh toán chi tiết
		/// </summary>
		public async Task<PaymentStatistics> GetPaymentStatisticsAsync(DateRangeStatisticsRequest request)
		{
			try
			{
				_logger.LogInformation("GET_PAYMENT_STATISTICS_START: Bắt đầu lấy thống kê thanh toán");

				var invoices = await _invoiceRepository.FindByPredicate(x =>
					x.DateCreated.HasValue &&
					x.DateCreated.Value >= request.StartDate &&
					x.DateCreated.Value <= request.EndDate &&
					!x.DeleteStatus);

				var totalRevenue = invoices.Sum(i => i.FinalPrice ?? 0);
				var totalPaidAmount = invoices.Sum(i => i.PaidAmount ?? 0);
				var totalOutstandingBalance = invoices.Sum(i => i.OutstandingBalance ?? 0);

				var fullPaymentCount = invoices.Count(i => i.Status == "DaThanhToan");
				var partialPaymentCount = invoices.Count(i => i.Status == "ThanhToanMotPhan");
				var unpaidCount = invoices.Count(i => i.Status == "ChuaThanhToan");

				// Phương thức thanh toán phổ biến
				var paymentMethods = invoices
					.GroupBy(i => i.PaymentMethod ?? "Unknown")
					.ToDictionary(g => g.Key, g => g.Count());

				var paymentRate = totalRevenue > 0 ? (double)(totalPaidAmount / totalRevenue) * 100 : 0;

				var result = new PaymentStatistics
				{
					TotalRevenue = totalRevenue,
					TotalPaidAmount = totalPaidAmount,
					TotalOutstandingBalance = totalOutstandingBalance,
					PaymentRate = paymentRate,
					FullyPaidInvoices = fullPaymentCount,
					PartiallyPaidInvoices = partialPaymentCount,
					UnpaidInvoices = unpaidCount,
					PaymentMethodStats = paymentMethods
				};

				_logger.LogInformation("GET_PAYMENT_STATISTICS_SUCCESS: TotalRevenue: {Revenue:C}, PaymentRate: {Rate:F2}%",
					totalRevenue, paymentRate);

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_PAYMENT_STATISTICS_EXCEPTION: Lỗi khi lấy thống kê thanh toán");
				return new PaymentStatistics();
			}
		}

		/// <summary>
		/// 🆕 Lấy thống kê khách hàng
		/// </summary>
		public async Task<CustomerStatistics> GetCustomerStatisticsAsync(DateRangeStatisticsRequest request)
		{
			try
			{
				_logger.LogInformation("GET_CUSTOMER_STATISTICS_START: Bắt đầu lấy thống kê khách hàng");

				var invoices = await _invoiceRepository.FindByPredicate(x =>
					x.DateCreated.HasValue &&
					x.DateCreated.Value >= request.StartDate &&
					x.DateCreated.Value <= request.EndDate &&
					!x.DeleteStatus);

				var totalCustomers = invoices.Select(i => i.CustomerId).Distinct().Count();
				var totalRevenue = invoices.Sum(i => i.FinalPrice ?? 0);
				var averageOrderValue = invoices.Count() > 0 ? totalRevenue / invoices.Count() : 0;

				// Khách hàng quay lại (mua nhiều lần)
				var customerPurchaseCount = invoices
					.GroupBy(i => i.CustomerId)
					.Where(g => g.Count() > 1)
					.ToList();

				var returningCustomers = customerPurchaseCount.Count;
				var newCustomers = totalCustomers - returningCustomers;
				var vipCustomers = customerPurchaseCount.Count(g => g.Sum(i => i.FinalPrice ?? 0) > averageOrderValue * 3);

				// Top 5 khách hàng chi tiêu nhiều
				var topCustomers = new List<TopCustomerStatistic>();
				var topCustomerIds = invoices
					.GroupBy(i => i.CustomerId)
					.Select(g => new
					{
						CustomerId = g.Key,
						TotalSpent = g.Sum(i => i.FinalPrice ?? 0),
						PurchaseCount = g.Count()
					})
					.OrderByDescending(x => x.TotalSpent)
					.Take(5)
					.ToList();

				foreach (var topCust in topCustomerIds)
				{
					if (topCust.CustomerId.HasValue)
					{
						try
						{
							var customer = await _customerRepository.GetById(topCust.CustomerId.Value);
							if (customer != null)
							{
								topCustomers.Add(new TopCustomerStatistic
								{
									CustomerId = customer.Id,
									CustomerName = customer.FullName,
									Email = customer.Email,
									Phone = customer.Phone,
									TotalSpent = topCust.TotalSpent,
									PurchaseCount = topCust.PurchaseCount,
									MembershipStatus = GetMembershipStatus(topCust.TotalSpent)
								});
							}
						}
						catch (Exception ex)
						{
							_logger.LogWarning(ex, "GET_CUSTOMER_STATISTICS_ITEM_ERROR: Lỗi khi xử lý khách hàng ID {CustomerId}",
								topCust.CustomerId);
						}
					}
				}

				var customerLifetimeValue = totalCustomers > 0 ? totalRevenue / totalCustomers : 0;

				var result = new CustomerStatistics
				{
					TotalCustomers = totalCustomers,
					NewCustomers = newCustomers,
					ReturningCustomers = returningCustomers,
					VIPCustomers = vipCustomers,
					AverageOrderValue = averageOrderValue,
					AverageCustomerLifetimeValue = customerLifetimeValue,
					TopCustomers = topCustomers
				};

				_logger.LogInformation("GET_CUSTOMER_STATISTICS_SUCCESS: TotalCustomers: {Total}, NewCustomers: {New}, ReturningCustomers: {Returning}",
					totalCustomers, newCustomers, returningCustomers);

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_CUSTOMER_STATISTICS_EXCEPTION: Lỗi khi lấy thống kê khách hàng");
				return new CustomerStatistics();
			}
		}

		/// <summary>
		/// 🆕 Lấy thống kê hàng tồn kho
		/// </summary>
		public async Task<InventoryStatistics> GetInventoryStatisticsAsync(DateRangeStatisticsRequest request)
		{
			try
			{
				_logger.LogInformation("GET_INVENTORY_STATISTICS_START: Bắt đầu lấy thống kê hàng tồn kho");

				var products = await _productRepository.FindByPredicate(x => !x.DeleteStatus);

				var totalProductsInStock = products.Sum(p => p.Quantity);
				var lowStockProducts = products.Count(p => p.Quantity < (p.MinimumStock) && p.Quantity > 0);
				var outOfStockProducts = products.Count(p => p.Quantity == 0);
				var inventoryValue = products.Sum(p => (p.CostPrice ?? 0) * p.Quantity);

				var lowStockList = products
					.Where(p => p.Quantity <= (p.MinimumStock))
					.OrderBy(p => p.Quantity)
					.Take(10)
					.Select(p => new LowStockProductStatistic
					{
						ProductId = p.Id,
						ProductName = p.ProductName,
						CurrentQuantity = p.Quantity,
						MinimumStock = p.MinimumStock,
						Status = p.Quantity == 0 ? "OutOfStock" : "LowStock",
						CostPrice = p.CostPrice ?? 0,
						StockValue = (p.CostPrice ?? 0) * p.Quantity
					})
					.ToList();

				var result = new InventoryStatistics
				{
					TotalProductsInStock = totalProductsInStock,
					LowStockProducts = lowStockProducts,
					OutOfStockProducts = outOfStockProducts,
					InventoryValue = inventoryValue,
					LowStockProductsList = lowStockList
				};

				_logger.LogInformation("GET_INVENTORY_STATISTICS_SUCCESS: TotalProducts: {Total}, LowStock: {Low}, OutOfStock: {Out}",
					totalProductsInStock, lowStockProducts, outOfStockProducts);

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_INVENTORY_STATISTICS_EXCEPTION: Lỗi khi lấy thống kê hàng tồn kho");
				return new InventoryStatistics();
			}
		}

		/// <summary>
		/// 🆕 Lấy thống kê hóa đơn theo trạng thái thanh toán
		/// </summary>
		public async Task<List<InvoiceStatusStatistic>> GetInvoiceStatusStatisticsAsync(DateRangeStatisticsRequest request)
		{
			try
			{
				_logger.LogInformation("GET_INVOICE_STATUS_STATISTICS_START: Bắt đầu lấy thống kê hóa đơn theo trạng thái");

				var invoices = await _invoiceRepository.FindByPredicate(x =>
					x.DateCreated.HasValue &&
					x.DateCreated.Value >= request.StartDate &&
					x.DateCreated.Value <= request.EndDate &&
					!x.DeleteStatus);

				var totalAmount = invoices.Sum(i => i.FinalPrice ?? 0);

				var statusGroups = invoices
					.GroupBy(i => i.Status ?? "Unknown")
					.Select(g => new InvoiceStatusStatistic
					{
						Status = g.Key,
						Count = g.Count(),
						TotalAmount = g.Sum(i => i.FinalPrice ?? 0),
						Percentage = totalAmount > 0 ? (double)(g.Sum(i => i.FinalPrice ?? 0) / totalAmount) * 100 : 0
					})
					.OrderByDescending(x => x.Count)
					.ToList();

				_logger.LogInformation("GET_INVOICE_STATUS_STATISTICS_SUCCESS: Lấy {Count} trạng thái hóa đơn",
					statusGroups.Count);

				return statusGroups;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GET_INVOICE_STATUS_STATISTICS_EXCEPTION: Lỗi khi lấy thống kê hóa đơn theo trạng thái");
				return new List<InvoiceStatusStatistic>();
			}
		}

		/// <summary>
		/// Helper: Xác định trạng thái thành viên dựa trên chi tiêu
		/// </summary>
		private string GetMembershipStatus(decimal totalSpent)
		{
			if (totalSpent >= 100_000_000) return "Platinum"; // 100M
			if (totalSpent >= 50_000_000) return "Gold";      // 50M
			if (totalSpent >= 20_000_000) return "Silver";    // 20M
			return "Normal";
		}
	}
	#endregion
}