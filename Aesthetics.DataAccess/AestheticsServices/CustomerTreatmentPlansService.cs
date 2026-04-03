using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Enum;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using LinqKit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
	public class CustomerTreatmentPlansService : ICustomerTreatmentPlansService
	{
		private readonly ILogger<CustomerTreatmentPlansService> _logger;
		private readonly ITreatmentPlanRepository _treatmentPlanRepository;
		private readonly ITreatmentSessionRepository _treatmentSessionRepository;
		private readonly ICustomerTreatmentPlansRepository _customerTreatmentPlansRepository;
		private readonly ICustomerTreatmentSessionsRepository _customerTreatmentSessionsRepository;
		private readonly IServiceRepository _serviceRepository;
		private readonly IInvoiceRepository _invoiceRepository;
		private readonly IInvoiceDetailsRepository _invoiceDetailsRepository;
		private readonly IProductRepository _productRepository;
		private readonly ISessionProductRepository _sessionProductRepository;
		private readonly IPerformanceLogRepository _performanceLogRepository;

		public CustomerTreatmentPlansService(ILogger<CustomerTreatmentPlansService> logger
			, ICustomerTreatmentPlansRepository customerTreatmentPlansRepository
			, ICustomerTreatmentSessionsRepository customerTreatmentSessionsRepository
			, ITreatmentPlanRepository treatmentPlanRepository
			, ITreatmentSessionRepository treatmentSessionRepository
			, IServiceRepository serviceRepository
			, IInvoiceRepository invoiceRepository
			, IInvoiceDetailsRepository invoiceDetailsRepository
			, IProductRepository productRepository
			, ISessionProductRepository sessionProductRepository
			, IPerformanceLogRepository performanceLogRepository)
		{
			_logger = logger;
			_customerTreatmentPlansRepository = customerTreatmentPlansRepository;
			_customerTreatmentSessionsRepository = customerTreatmentSessionsRepository;
			_treatmentPlanRepository = treatmentPlanRepository;
			_treatmentSessionRepository = treatmentSessionRepository;
			_serviceRepository = serviceRepository;
			_invoiceRepository = invoiceRepository;
			_invoiceDetailsRepository = invoiceDetailsRepository;
			_productRepository = productRepository;
			_sessionProductRepository = sessionProductRepository;
			_performanceLogRepository = performanceLogRepository;
		}

		public async Task<bool> create(CreateCustomerTreatment request)
		{
			try
			{
				if (!IsValidRequest(request))
					return false;

				// Xác định kiểu mua hàng dựa trên TreatmentSessionIds
				bool isFullPackage = request.TreatmentSessionIds == null || !request.TreatmentSessionIds.Any();

				if (isFullPackage)
				{
					// Mua toàn bộ gói liệu trình
					return await CreateFullPackageTreatment(request);
				}
				else
				{
					// Mua lẻ các buổi cụ thể
					return await CreateIndividualSessionsTreatment(request);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CreateCustomerTreatment exception");
				return false;
			}
		}

		/// <summary>
		/// Xử lý mua toàn bộ gói liệu trình
		/// </summary>
		private async Task<bool> CreateFullPackageTreatment(CreateCustomerTreatment request)
		{
			try
			{
				// Kiểm tra CustomerId hợp lệ
				if (!request.CustomerId.HasValue)
				{
					_logger.LogWarning("CreateFullPackageTreatment: CustomerId is required");
					return false;
				}

				var plan = await GetTreatmentPlan(request.TreatmentPlanId);
				if (plan == null)
				{
					_logger.LogWarning("CreateFullPackageTreatment: TreatmentPlan not found - Id: {Id}", request.TreatmentPlanId);
					return false;
				}

				var pricing = await GetUnitPrice(plan, true);
				if (!pricing.IsValid)
				{
					_logger.LogWarning("CreateFullPackageTreatment: Invalid pricing for plan {PlanId}", request.TreatmentPlanId);
					return false;
				}

				var entity = new CustomerTreatmentPlanEntity
				{
					CustomerId = request.CustomerId.Value,  
					TreatmentPlanId = request.TreatmentPlanId,
					Status = "ChoDatLich",
					DeleteStatus = false
				};

				await _customerTreatmentPlansRepository.CreateEntity(entity);

				// Clone tất cả buổi từ gói liệu trình
				await CloneTreatmentSessions(entity.Id, request.TreatmentPlanId);

				_logger.LogInformation(
					"CreateFullPackageTreatment: Success - CustomerId: {CustomerId}, TreatmentPlanId: {TreatmentPlanId}, CustomerTreatmentPlanId: {CustomerTreatmentPlanId}",
					request.CustomerId, request.TreatmentPlanId, entity.Id);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CreateFullPackageTreatment exception - CustomerId: {CustomerId}", request.CustomerId);
				return false;
			}
		}

		/// <summary>
		/// Xử lý mua lẻ các buổi cụ thể
		/// UI gửi TreatmentSessionIds → lấy TreatmentSession trực tiếp
		/// Tự động lấy TreatmentPlanId từ TreatmentSession
		/// </summary>
		private async Task<bool> CreateIndividualSessionsTreatment(CreateCustomerTreatment request)
		{
			try
			{
				// Kiểm tra CustomerId hợp lệ
				if (!request.CustomerId.HasValue)
				{
					_logger.LogWarning("CreateIndividualSessionsTreatment: CustomerId is required");
					return false;
				}

				if (request.TreatmentSessionIds == null || !request.TreatmentSessionIds.Any())
				{
					_logger.LogWarning(
						"CreateIndividualSessionsTreatment: Invalid request - TreatmentSessionIds is empty");
					return false;
				}

				// Lấy các TreatmentSession theo IDs
				var treatmentSessions = await _treatmentSessionRepository
					.FindByPredicate(x =>
						request.TreatmentSessionIds.Contains(x.Id)
						&& !x.DeleteStatus);

				if (!treatmentSessions.Any())
				{
					_logger.LogWarning(
						"CreateIndividualSessionsTreatment: No TreatmentSessions found for IDs: {SessionIds}",
						string.Join(", ", request.TreatmentSessionIds));
					return false;
				}

				// Kiểm tra số lượng buổi tìm được có khớp yêu cầu không
				if (treatmentSessions.Count() != request.TreatmentSessionIds.Count)
				{
					_logger.LogWarning(
						"CreateIndividualSessionsTreatment: Found {Found} sessions but {Requested} were requested",
						treatmentSessions.Count(), request.TreatmentSessionIds.Count);
					return false;
				}

				// Lấy TreatmentPlanId từ session đầu tiên (tất cả session phải cùng 1 plan)
				var firstSession = treatmentSessions.First();
				if (!firstSession.TreatmentPlanId.HasValue)
				{
					_logger.LogWarning(
						"CreateIndividualSessionsTreatment: TreatmentSession {SessionId} has no TreatmentPlanId",
						firstSession.Id);
					return false;
				}

				var treatmentPlanId = firstSession.TreatmentPlanId.Value;

				// Kiểm tra tất cả sessions phải thuộc cùng 1 plan
				if (!treatmentSessions.All(x => x.TreatmentPlanId == treatmentPlanId))
				{
					_logger.LogWarning(
						"CreateIndividualSessionsTreatment: Sessions belong to different plans");
					return false;
				}

				var plan = await GetTreatmentPlan(treatmentPlanId);
				if (plan == null)
				{
					_logger.LogWarning(
						"CreateIndividualSessionsTreatment: TreatmentPlan not found - Id: {Id}",
						treatmentPlanId);
					return false;
				}

				// Lấy giá từ gói liệu trình (mua lẻ dùng giá unit)
				var pricing = await GetUnitPrice(plan, false);
				if (!pricing.IsValid)
				{
					_logger.LogWarning(
						"CreateIndividualSessionsTreatment: Invalid pricing for plan {PlanId}",
						treatmentPlanId);
					return false;
				}

				// Tạo bản ghi CustomerTreatmentPlan cho lần mua lẻ này
				var entity = new CustomerTreatmentPlanEntity
				{
					CustomerId = request.CustomerId.Value,  // ✅ Sử dụng .Value vì đã kiểm tra
					TreatmentPlanId = treatmentPlanId,
					Status = "ChoDatLich",
					DeleteStatus = false
				};

				await _customerTreatmentPlansRepository.CreateEntity(entity);

				// Tạo các CustomerTreatmentSession cho các buổi được chỉ định
				await CreateCustomerSessionsForIndividualPurchase(
					entity.Id,
					treatmentSessions,
					request.StaffId);

				_logger.LogInformation(
					"CreateIndividualSessionsTreatment: Success - CustomerId: {CustomerId}, TreatmentPlanId: {TreatmentPlanId}, SessionCount: {SessionCount}, CustomerTreatmentPlanId: {CustomerTreatmentPlanId}",
					request.CustomerId, treatmentPlanId, treatmentSessions.Count(), entity.Id);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex,
					"CreateIndividualSessionsTreatment exception - CustomerId: {CustomerId}",
					request.CustomerId);
				return false;
			}
		}

		private bool IsValidRequest(CreateCustomerTreatment request)
		{
			if (request == null || !request.CustomerId.HasValue)
			{
				_logger.LogWarning("IsValidRequest: Invalid request - null or missing CustomerId");
				return false;
			}

			// Xác định loại mua hàng
			bool isFullPackage = request.TreatmentSessionIds == null || !request.TreatmentSessionIds.Any();

			if (isFullPackage)
			{
				// Mua gói: bắt buộc TreatmentPlanId
				if (!request.TreatmentPlanId.HasValue)
				{
					_logger.LogWarning("IsValidRequest: Missing TreatmentPlanId for full package purchase");
					return false;
				}
			}
			else
			{
				// Mua lẻ: bắt buộc TreatmentSessionIds, không cần TreatmentPlanId
				// (sẽ tự động lấy từ TreatmentSession)
				if (request.TreatmentSessionIds == null || !request.TreatmentSessionIds.Any())
				{
					_logger.LogWarning("IsValidRequest: Missing TreatmentSessionIds for individual session purchase");
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Tạo các buổi chữa trị cụ thể dựa trên danh sách TreatmentSessionIds
		/// </summary>
		private async Task<bool> CreateSpecificSessions(
			int customerTreatmentPlanId,
			int treatmentPlanId,
			List<int> treatmentSessionIds,
			int? staffId = null)
		{
			try
			{
				// Lấy các buổi template từ gói liệu trình dựa trên IDs
				var templateSessions = await _treatmentSessionRepository
					.FindByPredicate(x =>
						x.TreatmentPlanId == treatmentPlanId
						&& treatmentSessionIds.Contains(x.Id)
						&& !x.DeleteStatus);

				if (!templateSessions.Any())
				{
					_logger.LogWarning(
						"CreateSpecificSessions: No template sessions found matching the requested IDs for TreatmentPlanId: {TreatmentPlanId}",
						treatmentPlanId);
					return false;
				}

				// Kiểm tra số lượng buổi tìm được có khớp yêu cầu không
				if (templateSessions.Count() != treatmentSessionIds.Count)
				{
					_logger.LogWarning(
						"CreateSpecificSessions: Found {Found} sessions but {Requested} were requested for TreatmentPlanId: {TreatmentPlanId}",
						templateSessions.Count(), treatmentSessionIds.Count, treatmentPlanId);
					return false;
				}

				// Tạo CustomerTreatmentSession cho mỗi buổi được yêu cầu
				var sessionsToCreate = new List<CustomerTreatmentSessionEntity>();

				foreach (var templateSession in templateSessions.OrderBy(x => x.Id))
				{
					var customerSession = new CustomerTreatmentSessionEntity
					{
						CustomerTreatmentPlanId = customerTreatmentPlanId,
						TreatmentSessionId = templateSession.Id,
						Status = "ChoDatLich",
						DeleteStatus = false
					};

					sessionsToCreate.Add(customerSession);
				}

				if (sessionsToCreate.Any())
				{
					await _customerTreatmentSessionsRepository.CreateRangeEntities(sessionsToCreate);
					_logger.LogInformation(
						"CreateSpecificSessions: Created {Count} specific sessions for CustomerTreatmentPlanId: {CustomerTreatmentPlanId}",
						sessionsToCreate.Count, customerTreatmentPlanId);
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex,
					"CreateSpecificSessions exception - CustomerTreatmentPlanId: {CustomerTreatmentPlanId}, TreatmentPlanId: {TreatmentPlanId}",
					customerTreatmentPlanId, treatmentPlanId);
				return false;
			}
		}

		/// <summary>
		/// Tạo CustomerTreatmentSession từ danh sách TreatmentSession đã lấy
		/// </summary>
		private async Task<bool> CreateCustomerSessionsForIndividualPurchase(
			int customerTreatmentPlanId,
			IEnumerable<TreatmentSessionEntity> treatmentSessions,
			int? staffId = null)
		{
			try
			{
				var sessionsToCreate = new List<CustomerTreatmentSessionEntity>();

				foreach (var treatmentSession in treatmentSessions.OrderBy(x => x.SessionNumber))
				{
					var customerSession = new CustomerTreatmentSessionEntity
					{
						CustomerTreatmentPlanId = customerTreatmentPlanId,
						TreatmentSessionId = treatmentSession.Id,
						Status = "ChoDatLich",
						DeleteStatus = false
					};

					sessionsToCreate.Add(customerSession);
				}

				if (sessionsToCreate.Any())
				{
					await _customerTreatmentSessionsRepository.CreateRangeEntities(sessionsToCreate);
					_logger.LogInformation(
						"CreateCustomerSessionsForIndividualPurchase: Created {Count} sessions for CustomerTreatmentPlanId: {CustomerTreatmentPlanId}",
						sessionsToCreate.Count, customerTreatmentPlanId);
				}

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex,
					"CreateCustomerSessionsForIndividualPurchase exception - CustomerTreatmentPlanId: {CustomerTreatmentPlanId}",
					customerTreatmentPlanId);
				return false;
			}
		}


		private async Task<bool> CloneTreatmentSessions(int customerPlanId, int? treatmentPlanId)
		{
			try
			{
				if (!treatmentPlanId.HasValue)
				{
					_logger.LogWarning("CloneTreatmentSessions: treatmentPlanId is null");
					return false;
				}

				var templateSessions = await _treatmentSessionRepository
					.FindByPredicate(x => x.TreatmentPlanId == treatmentPlanId && !x.DeleteStatus);

				if (!templateSessions.Any())
				{
					_logger.LogWarning(
						"CloneTreatmentSessions: No TreatmentSessions found for TreatmentPlanId: {TreatmentPlanId}",
						treatmentPlanId);
					return false;
				}

				var sessions = templateSessions.Select(x => new CustomerTreatmentSessionEntity
				{
					CustomerTreatmentPlanId = customerPlanId,
					TreatmentSessionId = x.Id,
					Status = "ChoDatLich",
					DeleteStatus = false
				}).ToList();

				var created = await _customerTreatmentSessionsRepository.CreateRangeEntities(sessions);
				if (!created)
				{
					_logger.LogError(
						"CloneTreatmentSessions: Failed to create CustomerTreatmentSessions for CustomerTreatmentPlanId: {CustomerTreatmentPlanId}",
						customerPlanId);
					return false;
				}

				_logger.LogInformation(
					"CloneTreatmentSessions: Successfully created {Count} sessions for CustomerTreatmentPlanId: {CustomerTreatmentPlanId}",
					sessions.Count, customerPlanId);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex,
					"CloneTreatmentSessions exception - CustomerTreatmentPlanId: {CustomerTreatmentPlanId}, TreatmentPlanId: {TreatmentPlanId}",
					customerPlanId, treatmentPlanId);
				return false;
			}
		}

		public async Task<bool> delete(DeleteCustomerTreatment dto)
		{
			try
			{
				// ✅ Validate input
				if (dto == null || !dto.Id.HasValue)
				{
					_logger.LogWarning("DeleteCustomerTreatment: invalid payload");
					return false;
				}

				_logger.LogInformation("DeleteCustomerTreatment started: PlanId {PlanId}", dto.Id);

				// ✅ Get the plan
				var existing = (await _customerTreatmentPlansRepository.FindByPredicate(x => x.Id == dto.Id.Value)).FirstOrDefault();
				if (existing == null)
				{
					_logger.LogWarning("DeleteCustomerTreatment: not found id={Id}", dto.Id);
					return false;
				}

				// ✅ Get all related sessions before deleting the plan
				var relatedSessions = await _customerTreatmentSessionsRepository
					.FindByPredicate(x => x.CustomerTreatmentPlanId == dto.Id.Value && !x.DeleteStatus);

				_logger.LogInformation("DeleteCustomerTreatment: Found {Count} active sessions for plan {PlanId}",
					relatedSessions.Count(), dto.Id);

				// ✅ Delete all related sessions
				if (relatedSessions.Any())
				{
					foreach (var session in relatedSessions)
					{
						var sessionDeleted = await _customerTreatmentSessionsRepository.DeleteEntitiesStatus(session);
						if (!sessionDeleted)
						{
							_logger.LogError("DeleteCustomerTreatment: Failed to delete session {SessionId} for plan {PlanId}",
								session.Id, dto.Id);
							return false;
						}

						_logger.LogInformation("DeleteCustomerTreatment: Session {SessionId} deleted successfully",
							session.Id);
					}
				}

				// ✅ Delete the plan
				var planDeleted = await _customerTreatmentPlansRepository.DeleteEntitiesStatus(existing);
				if (!planDeleted)
				{
					_logger.LogError("DeleteCustomerTreatment: Failed to delete plan {PlanId}", dto.Id);
					return false;
				}

				_logger.LogInformation("DeleteCustomerTreatment: Success - Plan {PlanId} and {SessionCount} sessions deleted",
					dto.Id, relatedSessions.Count());

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "DeleteCustomerTreatment: exception for plan {PlanId}", dto.Id);
				return false;
			}
		}

		public async Task<bool> update(UpdateCustomerTreatment request)
		{
			try
			{
				var existing = await _customerTreatmentPlansRepository.GetById(request.Id.Value);
				if (existing == null)
				{
					_logger.LogWarning("UpdateCustomerTreatment: not found id={Id}", request.Id);
					return false;
				}

				bool hasChanges = false;
				string oldStatus = existing.Status;

				if (!string.IsNullOrWhiteSpace(request.Status) && existing.Status != request.Status)
				{
					existing.Status = request.Status;
					hasChanges = true;
				}

				if (!hasChanges)
				{
					_logger.LogInformation("UpdateCustomerTreatment: No changes detected for Id {Id}", request.Id);
					return true;
				}

				var updated = await _customerTreatmentPlansRepository.UpdateEntity(existing);
				if (!updated)
				{
					_logger.LogError("UpdateCustomerTreatment: Failed at repository level for Id {Id}", request.Id);
					return false;
				}

				if (request.Status != oldStatus)
				{
					await UpdateCustomerTreatmentSessionStatuses(existing.Id, request.Status, oldStatus);
				}

				_logger.LogInformation("UpdateCustomerTreatment: Success for Id {Id}", request.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UpdateCustomerTreatment: Exception for Id {Id}", request.Id);
				return false;
			}
		}


		public async Task<BaseDataCollection<CustomerTreatmentPlanResponseModel>> getlist(GetCustomerTreatment treatment)
		{
			try
			{
				Expression<Func<CustomerTreatmentPlanEntity, bool>> predicate = x => !x.DeleteStatus;

				// ✅ Lọc theo CustomerId (nếu có)
				if (treatment.CustomerId.HasValue)
				{
					predicate = predicate.And(x => x.CustomerId == treatment.CustomerId.Value);
					_logger.LogInformation("GETLIST_FILTER_CUSTOMER: CustomerId {CustomerId}", treatment.CustomerId.Value);
				}

				// ✅ Lọc theo Status (nếu có)
				if (!string.IsNullOrWhiteSpace(treatment.Status))
				{
					predicate = predicate.And(x => x.Status == treatment.Status);
					_logger.LogInformation("GETLIST_FILTER_STATUS: Status {Status}", treatment.Status);
				}

				var allMatching = await _customerTreatmentPlansRepository.FindByPredicate(predicate);
				var allMatchingList = allMatching.ToList();
				var totalCount = allMatchingList.Count;

				_logger.LogInformation("GETLIST_MATCHING: Found {Count} records after filtering", totalCount);

				// ✅ Batch load TreatmentPlans
				var treatmentPlanIds = allMatchingList
					.Where(x => x.TreatmentPlanId.HasValue)
					.Select(x => x.TreatmentPlanId.Value)
					.Distinct()
					.ToList();

				var treatmentPlansMap = new Dictionary<int, TreatmentPlanEntity>();
				if (treatmentPlanIds.Any())
				{
					var treatmentPlans = (await _treatmentPlanRepository.FindByPredicate(x =>
						treatmentPlanIds.Contains(x.Id) && !x.DeleteStatus))
						.ToList();

					treatmentPlansMap = treatmentPlans.ToDictionary(p => p.Id);
					_logger.LogInformation("GETLIST_TREATMENT_PLANS: Loaded {Count} treatment plans", treatmentPlansMap.Count);
				}

				// ✅ Batch load Services từ TreatmentPlans
				var serviceIds = treatmentPlansMap.Values
					.Where(x => x.ServiceId.HasValue)
					.Select(x => x.ServiceId.Value)
					.Distinct()
					.ToList();

				var servicesMap = new Dictionary<int, ServiceEntity>();
				if (serviceIds.Any())
				{
					var services = (await _serviceRepository.FindByPredicate(x =>
						serviceIds.Contains(x.Id) && !x.DeleteStatus))
						.ToList();

					servicesMap = services.ToDictionary(s => s.Id);
					_logger.LogInformation("GETLIST_SERVICES: Loaded {Count} services", servicesMap.Count);
				}

				// ✅ Batch load TreatmentSessions từ TreatmentPlans (không phải từ CustomerTreatmentSessions)
				var allTreatmentSessions = new Dictionary<int, TreatmentSessionEntity>();
				var allSessionProducts = new Dictionary<int, List<SessionProductEntity>>();

				if (treatmentPlanIds.Any())
				{
					// Lấy tất cả TreatmentSessions từ các TreatmentPlan
					var treatmentSessions = (await _treatmentSessionRepository
						.FindByPredicate(x => treatmentPlanIds.Contains(x.TreatmentPlanId ?? 0) && !x.DeleteStatus))
						.ToList();

					allTreatmentSessions = treatmentSessions.ToDictionary(ts => ts.Id);
					_logger.LogInformation("GETLIST_TREATMENT_SESSIONS: Loaded {Count} treatment sessions", allTreatmentSessions.Count);

					// ✅ Batch load SessionProducts
					var treatmentSessionIdList = allTreatmentSessions.Keys.ToList();

					if (treatmentSessionIdList.Any())
					{
						var sessionProductsList = (await _sessionProductRepository
							.FindByPredicate(x => treatmentSessionIdList.Contains(x.TreatmentSessionId ?? 0) && !x.DeleteStatus))
							.ToList();

						// ✅ Batch load Products
						var productIds = sessionProductsList
							.Where(sp => sp.ProductId.HasValue)
							.Select(sp => sp.ProductId.Value)
							.Distinct()
							.ToList();

						var productsMap = new Dictionary<int, ProductEntity>();
						if (productIds.Any())
						{
							var products = (await _productRepository.FindByPredicate(x =>
								productIds.Contains(x.Id) && !x.DeleteStatus))
								.ToList();

							productsMap = products.ToDictionary(p => p.Id);
							_logger.LogInformation("GETLIST_PRODUCTS: Loaded {Count} products", productsMap.Count);
						}

						// ✅ Assign Products to SessionProducts
						foreach (var sessionProduct in sessionProductsList)
						{
							if (sessionProduct.ProductId.HasValue && productsMap.TryGetValue(sessionProduct.ProductId.Value, out var product))
							{
								sessionProduct.Product = product;
							}
						}

						// Nhóm SessionProducts theo TreatmentSessionId
						allSessionProducts = sessionProductsList
							.GroupBy(x => x.TreatmentSessionId ?? 0)
							.ToDictionary(g => g.Key, g => g.ToList());

						// ✅ Gán SessionProducts vào TreatmentSessions
						foreach (var session in allTreatmentSessions.Values)
						{
							if (allSessionProducts.TryGetValue(session.Id, out var products))
							{
								session.SessionProducts = products;
							}
						}

						_logger.LogInformation("GETLIST_SESSION_PRODUCTS: Loaded {Count} session products", sessionProductsList.Count);
					}
				}

				// ✅ Batch load CustomerTreatmentSessions từ các CustomerTreatmentPlan (chỉ những chưa bị delete)
				var customerTreatmentPlanIds = allMatchingList
					.Select(x => x.Id)
					.Distinct()
					.ToList();

				var customerSessionsMap = new Dictionary<int, Dictionary<int, CustomerTreatmentSessionEntity>>();
				if (customerTreatmentPlanIds.Any())
				{
					var customerSessions = (await _customerTreatmentSessionsRepository
						.FindByPredicate(x => customerTreatmentPlanIds.Contains(x.CustomerTreatmentPlanId ?? 0) && !x.DeleteStatus))
						.ToList();

					// Nhóm theo CustomerTreatmentPlanId, sau đó theo TreatmentSessionId
					customerSessionsMap = customerSessions
						.GroupBy(x => x.CustomerTreatmentPlanId ?? 0)
						.ToDictionary(
							g => g.Key,
							g => g.ToDictionary(cs => cs.TreatmentSessionId ?? 0)
						);

					_logger.LogInformation("GETLIST_CUSTOMER_SESSIONS: Loaded {Count} customer treatment sessions", customerSessions.Count);
				}

				// ✅ Phân trang trước khi mapping
				var pagedData = allMatchingList
					.OrderByDescending(x => x.Id)
					.Skip((treatment.PageNo - 1) * treatment.PageSize)
					.Take(treatment.PageSize)
					.ToList();

				_logger.LogInformation("GETLIST_PAGINATION: PageNo {PageNo}, PageSize {PageSize}, Returned {Count}",
					treatment.PageNo, treatment.PageSize, pagedData.Count);

				// ✅ Map sang CustomerTreatmentPlanResponseModel
				var responseData = pagedData.Select(plan =>
					MapToResponseModel(plan, treatmentPlansMap, servicesMap, allTreatmentSessions, customerSessionsMap))
					.ToList();

				_logger.LogInformation("GETLIST_SUCCESS: Total {Total}, Returned {Returned}",
					totalCount, responseData.Count);

				return new BaseDataCollection<CustomerTreatmentPlanResponseModel>(
					responseData,
					totalCount,
					treatment.PageNo,
					treatment.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GETLIST_EXCEPTION: Get customer treatment plan list failed");
				return new BaseDataCollection<CustomerTreatmentPlanResponseModel>(
					null,
					0,
					treatment.PageNo,
					treatment.PageSize
				);
			}
		}

		/// <summary>
		/// Map CustomerTreatmentPlanEntity sang CustomerTreatmentPlanResponseModel
		/// </summary>
		private CustomerTreatmentPlanResponseModel MapToResponseModel(
			CustomerTreatmentPlanEntity entity,
			Dictionary<int, TreatmentPlanEntity> treatmentPlansMap,
			Dictionary<int, ServiceEntity> servicesMap,
			Dictionary<int, TreatmentSessionEntity> allTreatmentSessions,
			Dictionary<int, Dictionary<int, CustomerTreatmentSessionEntity>> customerSessionsMap)
		{
			var response = new CustomerTreatmentPlanResponseModel
			{
				CustomerTreatmentPlanInformation = new CustomerTreatmentPlanInformation
				{
					Id = entity.Id,
					CustomerId = entity.CustomerId,
					TreatmentPlanId = entity.TreatmentPlanId,
					Status = entity.Status
				}
			};

			// Map TreatmentPlanInformation
			if (entity.TreatmentPlanId.HasValue && treatmentPlansMap.TryGetValue(entity.TreatmentPlanId.Value, out var treatmentPlan))
			{
				response.TreatmentPlanInformation = new TreatmentPlanInfomation
				{
					Id = treatmentPlan.Id,
					DeleteStatus = treatmentPlan.DeleteStatus,
					ServiceId = treatmentPlan.ServiceId,
					PlanName = treatmentPlan.PlanName,
					TotalSessions = treatmentPlan.TotalSessions,
					Price = treatmentPlan.Price,
					SessionInterval = treatmentPlan.SessionInterval,
					Description = treatmentPlan.Description
				};

				// Map ServiceInformation từ TreatmentPlan
				if (treatmentPlan.ServiceId.HasValue && servicesMap.TryGetValue(treatmentPlan.ServiceId.Value, out var service))
				{
					response.ServiceInformation = new ServiceInfomation
					{
						ServiceName = service.ServiceName,
						ServiceId = service.Id,
						ServiceTypeId = service.ServiceTypeId,
						ServiceImage = service.ServiceImage,
						Price = service.Price,
						Duration = service.Duration,
						IsCourse = service.IsCourse
					};
				}
			}

			// ✅ Map CustomerSessions từ TreatmentSessions của gói (chỉ những chưa bị delete)
			var customerSessionsList = new List<CustomerSessionInformation>();

			if (entity.TreatmentPlanId.HasValue && treatmentPlansMap.TryGetValue(entity.TreatmentPlanId.Value, out var plan))
			{
				// Lấy tất cả TreatmentSessions thuộc gói này
				var planSessions = allTreatmentSessions.Values
					.Where(x => x.TreatmentPlanId == plan.Id)
					.OrderBy(x => x.SessionNumber)
					.ToList();

				// ✅ Lấy CustomerTreatmentSessions của plan này
				var planCustomerSessions = new Dictionary<int, CustomerTreatmentSessionEntity>();
				if (customerSessionsMap.TryGetValue(entity.Id, out var customerSessions))
				{
					planCustomerSessions = customerSessions;
				}

				foreach (var treatmentSession in planSessions)
				{
					// ✅ Kiểm tra nếu có CustomerTreatmentSession (chưa bị delete)
					if (planCustomerSessions.TryGetValue(treatmentSession.Id, out var customerSession))
					{
						var customerSessionInfo = new CustomerSessionInformation
						{
							CustomerSessionId = customerSession.Id,
							TreatmentSessionId = treatmentSession.Id,
							SessionNumber = treatmentSession.SessionNumber,
							SessionName = treatmentSession.SessionName,
							Description = treatmentSession.Description,
							Duration = treatmentSession.Duration,
							Status = customerSession.Status, // ✅ Lấy status từ CustomerTreatmentSession thực tế
							Products = new List<SessionProductInformation>()
						};

						// ✅ Map Products từ SessionProducts
						if (treatmentSession.SessionProducts != null && treatmentSession.SessionProducts.Any())
						{
							foreach (var sessionProduct in treatmentSession.SessionProducts)
							{
								var productInfo = new SessionProductInformation
								{
									SessionProductId = sessionProduct.Id,
									ProductId = sessionProduct.ProductId,
									ProductName = sessionProduct.Product?.ProductName,
									QuantityUsed = sessionProduct.QuantityUsed,
									ServiceId = sessionProduct.ServiceId
								};

								customerSessionInfo.Products.Add(productInfo);
							}
						}

						customerSessionsList.Add(customerSessionInfo);
					}
				}
			}

			response.CustomerSessions = customerSessionsList;

			return response;
		}

		private async Task<TreatmentPlanEntity?> GetTreatmentPlan(int? planId)
		{
			if (!planId.HasValue)
				return null;

			return (await _treatmentPlanRepository
				.FindByPredicate(x => x.Id == planId))
				.FirstOrDefault();
		}


		private async Task<(bool IsValid, decimal Price, int? ServiceId)> GetUnitPrice(
			TreatmentPlanEntity plan,
			bool isFullPackage)
		{
			if (isFullPackage)
			{
				var service = (await _serviceRepository
					.FindByPredicate(x => x.Id == plan.ServiceId))
					.FirstOrDefault();

				if (service == null)
					return (false, 0, null);

				return (true, service.Price ?? 0, service.Id);
			}

			return (true, plan.Price ?? 0, null);
		}

		/// <summary>
		/// Cập nhật status của CustomerTreatmentSession khi status của CustomerTreatmentPlan thay đổi
		/// </summary>
		private async Task UpdateCustomerTreatmentSessionStatuses(int customerTreatmentPlanId, string newPlanStatus, string oldPlanStatus)
		{
			try
			{
				// Lấy tất cả customer treatment sessions của plan này
				var customerSessions = await _customerTreatmentSessionsRepository
					.FindByPredicate(x => x.CustomerTreatmentPlanId == customerTreatmentPlanId && !x.DeleteStatus);

				if (!customerSessions.Any())
				{
					_logger.LogInformation("UpdateCustomerTreatmentSessionStatuses: No sessions found for CustomerTreatmentPlan {Id}", customerTreatmentPlanId);
					return;
				}

				// Xác định status mới cho sessions dựa trên status của plan
				string newSessionStatus = GetSessionStatusFromPlanStatus(newPlanStatus);

				if (string.IsNullOrEmpty(newSessionStatus))
				{
					_logger.LogWarning("UpdateCustomerTreatmentSessionStatuses: No mapping found for plan status {Status}", newPlanStatus);
					return;
				}

				var sessionsToUpdate = new List<CustomerTreatmentSessionEntity>();

				foreach (var session in customerSessions)
				{
					// Chỉ update những sessions chưa hoàn thành hoặc bỏ lỡ
					// trừ khi plan bị hủy hoặc tạm dừng
					if (ShouldUpdateSessionStatus(session.Status, newPlanStatus))
					{
						session.Status = newSessionStatus;
						sessionsToUpdate.Add(session);
					}
				}

				if (sessionsToUpdate.Any())
				{
					await _customerTreatmentSessionsRepository.UpdateRangeEntities(sessionsToUpdate);
					_logger.LogInformation("UpdateCustomerTreatmentSessionStatuses: Updated {Count} sessions to status {Status} for CustomerTreatmentPlan {Id}",
						sessionsToUpdate.Count, newSessionStatus, customerTreatmentPlanId);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UpdateCustomerTreatmentSessionStatuses: Exception for CustomerTreatmentPlan {Id}", customerTreatmentPlanId);
			}
		}

		/// <summary>
		/// Map status của CustomerTreatmentPlan sang status tương ứng của CustomerTreatmentSession
		/// </summary>
		private string GetSessionStatusFromPlanStatus(string planStatus)
		{
			return planStatus switch
			{
				"ChoDatLich" => "ChuaThucHien",      // Plan chờ đặt lịch -> Sessions chưa thực hiện
				"DangThucHien" => "ChuaThucHien",    // Plan đang thực hiện -> Sessions sẵn sàng nhưng chưa thực hiện
				"HoanThanh" => "HoanThanh",          // Plan hoàn thành -> Tất cả sessions hoàn thành
				"TamDung" => "ChuaThucHien",         // Plan tạm dừng -> Sessions trở về chưa thực hiện
				"Huy" => "ChuaThucHien",             // Plan hủy -> Sessions reset về chưa thực hiện
				_ => string.Empty
			};
		}

		/// <summary>
		/// Xác định có nên update status của session hay không dựa trên status hiện tại và status mới của plan
		/// </summary>
		private bool ShouldUpdateSessionStatus(string currentSessionStatus, string newPlanStatus)
		{
			// Luôn update nếu plan bị hủy hoặc tạm dừng
			if (newPlanStatus == "Huy" || newPlanStatus == "TamDung")
				return true;

			// Không override sessions đã hoàn thành hoặc bỏ lỡ
			if (currentSessionStatus == "HoanThanh" || currentSessionStatus == "BoLo")
				return false;

			// Khi plan hoàn thành, update tất cả sessions chưa hoàn thành
			if (newPlanStatus == "HoanThanh")
				return true;

			// Các trường hợp khác, update sessions chưa hoàn thành cụ thể
			return currentSessionStatus != "HoanThanh" && currentSessionStatus != "BoLo";
		}
	}
}
