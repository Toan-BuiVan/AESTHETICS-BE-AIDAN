using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices;
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
    public class TreatmentPlanService : ITreatmentPlanService
	{
		private readonly ILogger<TreatmentPlanService> _logger;
		private readonly ITreatmentPlanRepository _treatmentPlanRepository;
		private readonly ITreatmentSessionRepository _treatmentSessionRepository;
		private readonly ISessionProductRepository _sessionProductRepository;
		private readonly IProductRepository _productRepository;
		private readonly IServiceRepository _serviceRepository;

		public TreatmentPlanService(ILogger<TreatmentPlanService> logger
			, ITreatmentPlanRepository treatmentPlanRepository
			, ITreatmentSessionRepository treatmentSessionRepository
			, ISessionProductRepository sessionProductRepository
			, IProductRepository productRepository
			, IServiceRepository serviceRepository)
		{
			_logger = logger;
			_treatmentPlanRepository = treatmentPlanRepository;
			_treatmentSessionRepository = treatmentSessionRepository;
			_sessionProductRepository = sessionProductRepository;
			_productRepository = productRepository;
			_serviceRepository = serviceRepository;
		}

		public async Task<bool> create(CreateTreatmentPlan plan)
		{
			try
			{
				if (!plan.ServiceId.HasValue || string.IsNullOrWhiteSpace(plan.PlanName))
				{
					_logger.LogWarning("Create TreatmentPlan failed: Missing ServiceId or PlanName");
					return false;
				}

				var totalSessions = plan.TotalSessions ?? 0;

				var entity = new TreatmentPlanEntity
				{
					ServiceId = plan.ServiceId.Value,
					PlanName = plan.PlanName,
					TotalSessions = totalSessions,
					Price = plan.Price ?? 0,
					SessionInterval = plan.SessionInterval ?? 0,
					Description = plan.Description,
					DeleteStatus = false
				};

				var created = await _treatmentPlanRepository.CreateEntity(entity);

				if (!created)
				{
					_logger.LogError("Create TreatmentPlan failed at repository level: PlanName {PlanName}", plan.PlanName);
					return false;
				}

				if (totalSessions > 0)
				{
					var sessions = new List<TreatmentSessionEntity>();

					for (int i = 1; i <= totalSessions; i++)
					{
						sessions.Add(new TreatmentSessionEntity
						{
							TreatmentPlanId = entity.Id,
							SessionNumber = i,
							DeleteStatus = false
						});
					}

					var sessionsCreated = await _treatmentSessionRepository.CreateRangeEntities(sessions);
					if (!sessionsCreated)
					{
						_logger.LogError("Create TreatmentSessions failed at repository level: PlanName {PlanName}", plan.PlanName);
						return false;
					}

					// LOAD lại sessions từ database để lấy Id
					var createdSessions = (await _treatmentSessionRepository
						.FindByPredicate(x => x.TreatmentPlanId == entity.Id && !x.DeleteStatus))
						.ToList();

					// Tạo SessionProducts cho mỗi session nếu có SessionProducts
					if (plan.SessionProducts != null && plan.SessionProducts.Any())
					{
						await CreateSessionProductsForSessions(createdSessions, plan.SessionProducts, plan.ServiceId.Value);
					}
				}

				_logger.LogInformation(
					"Create TreatmentPlan success: PlanName {PlanName} for ServiceId {ServiceId}",
					plan.PlanName,
					plan.ServiceId);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create TreatmentPlan exception: PlanName {PlanName}", plan.PlanName);
				return false;
			}
		}

		public async Task<bool> delete(DeleteTreatmentPlan plan)
		{
			try
			{
				_logger.LogInformation("Start deleting TreatmentPlan");

				if (!plan.Id.HasValue)
				{
					_logger.LogWarning("Delete TreatmentPlan failed: Missing Id");
					return false;
				}

				var existingPlan = await _treatmentPlanRepository.GetById(plan.Id.Value);
				if (existingPlan == null)
				{
					_logger.LogWarning("Delete TreatmentPlan failed: Not found with Id {Id}", plan.Id);
					return false;
				}

				var deleted = await _treatmentPlanRepository.DeleteRangeEntitiesStatus(existingPlan);
				if (!deleted)
				{
					_logger.LogError("Delete TreatmentPlan failed at repository level: Id {Id}", plan.Id);
					return false;
				}

				_logger.LogInformation("Delete TreatmentPlan success: Id {Id}", plan.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete TreatmentPlan exception: Id {Id}", plan.Id);
				return false;
			}
		}

		public async Task<BaseDataCollection<TreatmentPlanResponseModel>> getlist(TreatmentPlanGet searchPlan)
		{
			try
			{
				Expression<Func<TreatmentPlanEntity, bool>> predicate = x => x.DeleteStatus != true;

				if (searchPlan.Id.HasValue)
				{
					predicate = x => x.Id == searchPlan.Id.Value && x.DeleteStatus != true;
				}
				else if (searchPlan.ServiceId.HasValue)
				{
					predicate = x => x.ServiceId == searchPlan.ServiceId.Value && x.DeleteStatus != true;
				}

				var allMatching = await _treatmentPlanRepository.FindByPredicate(predicate);
				var allMatchingList = allMatching.ToList();
				var totalCount = allMatchingList.Count;

				// ✅ Batch load Services
				var serviceIds = allMatchingList
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
				}

				// ✅ Assign Services to TreatmentPlans
				foreach (var plan in allMatchingList)
				{
					if (plan.ServiceId.HasValue && servicesMap.TryGetValue(plan.ServiceId.Value, out var service))
					{
						plan.Service = service;
					}
				}

				// ✅ Batch load TreatmentSessions liên quan
				var treatmentPlanIds = allMatchingList
					.Select(x => x.Id)
					.Distinct()
					.ToList();

				var allSessions = new Dictionary<int, List<TreatmentSessionEntity>>();

				if (treatmentPlanIds.Any())
				{
					var sessionsList = (await _treatmentSessionRepository
						.FindByPredicate(x => treatmentPlanIds.Contains(x.TreatmentPlanId ?? 0) && !x.DeleteStatus))
						.ToList();

					// ✅ Lấy tất cả SessionIds để batch load SessionProducts
					var sessionIds = sessionsList
						.Select(x => x.Id)
						.Distinct()
						.ToList();

					// ✅ Batch load SessionProducts WITH Product navigation
					var allSessionProducts = new Dictionary<int, List<SessionProductEntity>>();
					if (sessionIds.Any())
					{
						var sessionProductsList = (await _sessionProductRepository
							.FindByPredicate(x => sessionIds.Contains(x.TreatmentSessionId ?? 0) && !x.DeleteStatus))
							.ToList();

						// ✅ Load Products for SessionProducts
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
					}

					// ✅ Gán SessionProducts vào mỗi TreatmentSession
					foreach (var session in sessionsList)
					{
						if (allSessionProducts.TryGetValue(session.Id, out var products))
						{
							session.SessionProducts = products;
						}
					}

					// Nhóm sessions theo TreatmentPlanId
					allSessions = sessionsList
						.GroupBy(x => x.TreatmentPlanId ?? 0)
						.ToDictionary(g => g.Key, g => g.OrderBy(x => x.SessionNumber).ToList());
				}

				// ✅ Phân trang trước khi mapping
				var pagedData = allMatchingList
					.OrderBy(x => x.PlanName)
					.Skip((searchPlan.PageNo - 1) * searchPlan.PageSize)
					.Take(searchPlan.PageSize)
					.ToList();

				// ✅ Map TreatmentPlanEntity sang TreatmentPlanResponseModel
				var responseData = pagedData.Select(plan => MapToResponseModel(plan, allSessions)).ToList();

				_logger.LogInformation(
					"GetList TreatmentPlan success: Total {Total}, Returned {Returned}",
					totalCount, responseData.Count);

				return new BaseDataCollection<TreatmentPlanResponseModel>(
					responseData,
					totalCount,
					searchPlan.PageNo,
					searchPlan.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList TreatmentPlan exception");
				return new BaseDataCollection<TreatmentPlanResponseModel>(
					null,
					0,
					searchPlan.PageNo,
					searchPlan.PageSize
				);
			}
		}

		private TreatmentPlanResponseModel MapToResponseModel(TreatmentPlanEntity entity, Dictionary<int, List<TreatmentSessionEntity>> allSessions)
		{
			var response = new TreatmentPlanResponseModel
			{
				TreatmentPlanInfomation = new TreatmentPlanInfomation
				{
					Id = entity.Id,
					DeleteStatus = entity.DeleteStatus,
					ServiceId = entity.ServiceId,
					PlanName = entity.PlanName,
					TotalSessions = entity.TotalSessions,
					Price = entity.Price,
					SessionInterval = entity.SessionInterval,
					Description = entity.Description
				}
			};

			// Map ServiceInformation
			if (entity.Service != null)
			{
				response.ServiceInformation = new ServiceInfomation
				{
					ServiceName = entity.Service.ServiceName,
					ServiceId = entity.Service.Id,
					ServiceImage = entity.Service.ServiceImage,
					Price = entity.Service.Price,
					Duration = entity.Service.Duration,
					IsCourse = entity.Service.IsCourse
				};
			}

			// Map TreatmentSessionInformation and SessionProductInformation
			var sessionProductList = new List<SessionProductInformation>();

			if (allSessions.TryGetValue(entity.Id, out var sessions))
			{
				response.TreatmentSessionInformation = sessions
					.Select(session => new TreatmentSessionInformation
					{
						TreatmentSessionId = session.Id,
						SessionNumber = session.SessionNumber,
						SessionName = session.SessionName,
						Description = session.Description,
						Duration = session.Duration
					})
					.ToList();

				// Map SessionProductInformation from all sessions
				foreach (var session in sessions)
				{
					if (session.SessionProducts != null && session.SessionProducts.Any())
					{
						foreach (var sessionProduct in session.SessionProducts)
						{
							sessionProductList.Add(new SessionProductInformation
							{
								SessionProductId = sessionProduct.Id,
								ProductId = sessionProduct.ProductId,
								ProductName = sessionProduct.Product?.ProductName,
								QuantityUsed = sessionProduct.QuantityUsed,
								ServiceId = sessionProduct.ServiceId
							});
						}
					}
				}
			}
			else
			{
				response.TreatmentSessionInformation = new List<TreatmentSessionInformation>();
			}

			response.SessionProductInformation = sessionProductList;

			return response;
		}

		public async Task<bool> update(UpdateTreatmentPlan plan)
		{
			try
			{
				_logger.LogInformation("Start updating TreatmentPlan - Id: {Id}", plan.Id);

				if (!plan.Id.HasValue)
				{
					_logger.LogWarning("Update TreatmentPlan failed: Missing Id");
					return false;
				}

				var existingPlan = await _treatmentPlanRepository.GetById(plan.Id.Value);
				if (existingPlan == null)
				{
					_logger.LogWarning("Update TreatmentPlan failed: Not found with Id {Id}", plan.Id);
					return false;
				}

				var oldTotalSessions = existingPlan.TotalSessions ?? 0;

				bool hasChanges = false;

				if (plan.ServiceId.HasValue && existingPlan.ServiceId != plan.ServiceId.Value)
				{
					existingPlan.ServiceId = plan.ServiceId.Value;
					hasChanges = true;
					_logger.LogInformation("TreatmentPlan {Id}: ServiceId changed from {Old} to {New}",
						plan.Id, existingPlan.ServiceId, plan.ServiceId);
				}

				if (plan.TotalSessions.HasValue && existingPlan.TotalSessions != plan.TotalSessions.Value)
				{
					existingPlan.TotalSessions = plan.TotalSessions.Value;
					hasChanges = true;
					_logger.LogInformation("TreatmentPlan {Id}: TotalSessions changed from {Old} to {New}",
						plan.Id, oldTotalSessions, plan.TotalSessions);
				}

				if (plan.Price.HasValue && existingPlan.Price != plan.Price.Value)
				{
					existingPlan.Price = plan.Price.Value;
					hasChanges = true;
					_logger.LogInformation("TreatmentPlan {Id}: Price changed from {Old} to {New}",
						plan.Id, existingPlan.Price, plan.Price);
				}

				if (plan.SessionInterval.HasValue && existingPlan.SessionInterval != plan.SessionInterval.Value)
				{
					existingPlan.SessionInterval = plan.SessionInterval.Value;
					hasChanges = true;
					_logger.LogInformation("TreatmentPlan {Id}: SessionInterval changed from {Old} to {New}",
						plan.Id, existingPlan.SessionInterval, plan.SessionInterval);
				}

				if (!string.IsNullOrWhiteSpace(plan.Description) && existingPlan.Description != plan.Description)
				{
					existingPlan.Description = plan.Description;
					hasChanges = true;
					_logger.LogInformation("TreatmentPlan {Id}: Description updated", plan.Id);
				}

				if (!hasChanges)
				{
					_logger.LogInformation("Update TreatmentPlan: No changes detected for Id {Id}", plan.Id);
					return true;
				}

				var updated = await _treatmentPlanRepository.UpdateEntity(existingPlan);

				if (!updated)
				{
					_logger.LogError("Update TreatmentPlan failed at repository level: Id {Id}", plan.Id);
					return false;
				}

				// ✅ THÊM LOGGING: HANDLE SESSION CHANGES
				if (plan.TotalSessions.HasValue && plan.TotalSessions.Value != oldTotalSessions)
				{
					var sessions = (await _treatmentSessionRepository
						.FindByPredicate(x => x.TreatmentPlanId == existingPlan.Id && !x.DeleteStatus))
						.OrderBy(x => x.SessionNumber)
						.ToList();

					int newTotal = plan.TotalSessions.Value;

					// CASE 1: Increase sessions
					if (newTotal > oldTotalSessions)
					{
						_logger.LogInformation("TreatmentPlan {Id}: Increasing sessions from {Old} to {New}",
							plan.Id, oldTotalSessions, newTotal);

						var newSessions = new List<TreatmentSessionEntity>();

						for (int i = oldTotalSessions + 1; i <= newTotal; i++)
						{
							newSessions.Add(new TreatmentSessionEntity
							{
								TreatmentPlanId = existingPlan.Id,
								SessionNumber = i,
								DeleteStatus = false
							});
						}

						var sessionsCreated = await _treatmentSessionRepository.CreateRangeEntities(newSessions);
						if (sessionsCreated)
						{
							_logger.LogInformation("Created {Count} new TreatmentSessions", newSessions.Count);

							var loadedNewSessions = (await _treatmentSessionRepository
								.FindByPredicate(x => x.TreatmentPlanId == existingPlan.Id && x.SessionNumber > oldTotalSessions))
								.ToList();

							await CreateSessionProductsForNewSessions(loadedNewSessions, existingPlan.ServiceId.Value);
						}
					}

					// CASE 2: Decrease sessions
					else if (newTotal < oldTotalSessions)
					{
						_logger.LogInformation("TreatmentPlan {Id}: Decreasing sessions from {Old} to {New}",
							plan.Id, oldTotalSessions, newTotal);

						var sessionsToDelete = sessions
							.Where(x => x.SessionNumber.HasValue && x.SessionNumber.Value > newTotal)
							.ToList();

						if (sessionsToDelete.Any())
						{
							foreach (var session in sessionsToDelete)
							{
								session.DeleteStatus = true;
							}

							await _treatmentSessionRepository.UpdateRangeEntities(sessionsToDelete);
							_logger.LogInformation("Soft-deleted {Count} TreatmentSessions", sessionsToDelete.Count);

							var sessionIdsToDelete = sessionsToDelete.Select(s => s.Id).ToList();
							await DeleteSessionProductsForSessions(sessionIdsToDelete);
						}
					}
				}

				_logger.LogInformation("Update TreatmentPlan success: Id {Id}", plan.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update TreatmentPlan exception: Id {Id}", plan.Id);
				return false;
			}
		}

		private async Task CreateSessionProductsForSessions(List<TreatmentSessionEntity> sessions, List<SessionProductDefinition> sessionProductDefinitions, int serviceId)
		{
			try
			{
				if (!sessions.Any() || !sessionProductDefinitions.Any())
				{
					_logger.LogWarning("CreateSessionProductsForSessions: Empty sessions or sessionProductDefinitions");
					return;
				}

				var sessionProducts = new List<SessionProductEntity>();

				foreach (var session in sessions)
				{
					if (!session.SessionNumber.HasValue)
					{
						_logger.LogWarning("Session {SessionId} has no SessionNumber", session.Id);
						continue;
					}

					// Tìm định nghĩa sản phẩm cho session này
					var sessionDef = sessionProductDefinitions.FirstOrDefault(sp => sp.SessionNumber == session.SessionNumber.Value);

					if (sessionDef != null && sessionDef.Products.Any())
					{
						foreach (var productItem in sessionDef.Products)
						{
							_logger.LogInformation(
								"Adding SessionProduct: SessionId={SessionId}, ProductId={ProductId}, Quantity={Quantity}",
								session.Id, productItem.ProductId, productItem.QuantityUsed);

							sessionProducts.Add(new SessionProductEntity
							{
								TreatmentSessionId = session.Id,
								ProductId = productItem.ProductId,
								QuantityUsed = productItem.QuantityUsed,  
								ServiceId = serviceId,
								DeleteStatus = false
							});
						}
					}
					else
					{
						_logger.LogInformation(
							"No products defined for SessionNumber {SessionNumber}",
							session.SessionNumber);
					}
				}

				if (sessionProducts.Any())
				{
					_logger.LogInformation("About to create {Count} SessionProducts", sessionProducts.Count);
					var created = await _sessionProductRepository.CreateRangeEntities(sessionProducts);

					if (!created)
					{
						_logger.LogError("CreateRangeEntities failed for SessionProducts");
					}
					else
					{
						_logger.LogInformation(
							"✅ Successfully created {Count} SessionProducts for {SessionCount} sessions",
							sessionProducts.Count,
							sessions.Count);
					}
				}
				else
				{
					_logger.LogWarning("No SessionProducts to create after processing");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to create SessionProducts for sessions");
				throw;
			}
		}

		private async Task CreateSessionProductsForNewSessions(List<TreatmentSessionEntity> newSessions, int serviceId)
		{
			try
			{
				// Lấy SessionProducts từ sessions cũ để tạo cho sessions mới
				var existingSessionProducts = await _sessionProductRepository.FindByPredicate(x =>
					x.TreatmentSession!.TreatmentPlanId == newSessions.First().TreatmentPlanId && !x.DeleteStatus);

				if (!existingSessionProducts.Any())
				{
					_logger.LogInformation("No existing SessionProducts found to replicate for new sessions");
					return;
				}

				// Nhóm theo SessionNumber để tái tạo cấu trúc SessionProductDefinition
				var sessionProductDefinitions = existingSessionProducts
					.GroupBy(sp => sp.TreatmentSession!.SessionNumber)
					.Where(g => g.Key.HasValue)
					.Select(g => new SessionProductDefinition
					{
						SessionNumber = g.Key!.Value,
						Products = g.Where(sp => sp.ProductId.HasValue)
							.Select(sp => new SessionProductItem
							{
								ProductId = sp.ProductId!.Value,
								QuantityUsed = sp.QuantityUsed ?? 1
							}).ToList()
					})
					.ToList();

				if (sessionProductDefinitions.Any())
				{
					await CreateSessionProductsForSessions(newSessions, sessionProductDefinitions, serviceId);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to create SessionProducts for new sessions");
			}
		}

		private async Task DeleteSessionProductsForSessions(List<int> sessionIds)
		{
			try
			{
				var sessionProducts = await _sessionProductRepository.FindByPredicate(x => 
					sessionIds.Contains(x.TreatmentSessionId ?? 0) && !x.DeleteStatus);

				foreach (var sessionProduct in sessionProducts)
				{
					sessionProduct.DeleteStatus = true;
				}

				if (sessionProducts.Any())
				{
					await _sessionProductRepository.UpdateRangeEntities(sessionProducts);
					_logger.LogInformation("Deleted {Count} SessionProducts for deleted sessions", sessionProducts.Count());
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete SessionProducts for sessions");
			}
		}
	}
}
