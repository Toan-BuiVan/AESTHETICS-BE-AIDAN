using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
	public class TreatmentSessionService : ITreatmentSessionService
	{
		private readonly ILogger<TreatmentSessionService> _logger;
		private readonly ITreatmentSessionRepository _treatmentSessionRepository;
		private readonly ITreatmentPlanRepository _treatmentPlanRepository;

		public TreatmentSessionService(ILogger<TreatmentSessionService> logger,
			ITreatmentSessionRepository treatmentSessionRepository,
			ITreatmentPlanRepository treatmentPlanRepository)
		{
			_logger = logger;
			_treatmentSessionRepository = treatmentSessionRepository;
			_treatmentPlanRepository = treatmentPlanRepository;
		}

		public async Task<bool> create(CreateTreatmentSession session)
		{
			try
			{
				if (!session.TreatmentPlanId.HasValue)
				{
					_logger.LogWarning("Create TreatmentSession failed: Missing TreatmentPlanId");
					return false;
				}

				var plan = await _treatmentPlanRepository.GetById(session.TreatmentPlanId.Value);

				if (plan == null)
				{
					_logger.LogWarning("Create TreatmentSession failed: TreatmentPlan not found {PlanId}", session.TreatmentPlanId);
					return false;
				}

				var existingSessions = await _treatmentSessionRepository
					.FindByPredicate(x => x.TreatmentPlanId == session.TreatmentPlanId.Value && x.DeleteStatus != true);

				if (existingSessions.Count >= plan.TotalSessions)
				{
					_logger.LogWarning(
						"Create TreatmentSession failed: Exceed TotalSessions. PlanId {PlanId}, TotalSessions {Total}, Current {Current}",
						session.TreatmentPlanId,
						plan.TotalSessions,
						existingSessions.Count);

					return false;
				}

				if (session.SessionNumber > plan.TotalSessions)
				{
					return false;
				}

				var entity = new TreatmentSessionEntity
				{
					TreatmentPlanId = session.TreatmentPlanId.Value,
					SessionNumber = session.SessionNumber ?? existingSessions.Count + 1,
					SessionName = session.SessionName,
					Description = session.Description,
					Duration = session.Duration ?? 0,
					DeleteStatus = false
				};

				var created = await _treatmentSessionRepository.CreateEntity(entity);

				if (!created)
				{
					_logger.LogError("Create TreatmentSession failed at repository level");
					return false;
				}

				_logger.LogInformation("Create TreatmentSession success for PlanId {PlanId}", session.TreatmentPlanId);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create TreatmentSession exception");
				return false;
			}
		}

		public async Task<bool> update(UpdateTreatmentSession session)
		{
			try
			{
				if (!session.Id.HasValue)
				{
					_logger.LogWarning("Update TreatmentSession failed: Missing Id");
					return false;
				}

				var existing = await _treatmentSessionRepository.GetById(session.Id.Value);

				if (existing == null)
				{
					_logger.LogWarning("Update TreatmentSession failed: Not found Id {Id}", session.Id);
					return false;
				}

				bool hasChanges = false;

				if (session.SessionNumber.HasValue && existing.SessionNumber != session.SessionNumber.Value)
				{
					existing.SessionNumber = session.SessionNumber.Value;
					
					var plan = await _treatmentPlanRepository.GetById(session.TreatmentPlanId.Value);
					if (plan == null)
					{
						if (session.SessionNumber > plan.TotalSessions)
						{
							return false;
						}
					}
					hasChanges = true;
				}
				if (!string.IsNullOrWhiteSpace(session.SessionName) && existing.SessionName != session.SessionName)
				{
					existing.SessionName = session.SessionName;
					hasChanges = true;
				}

				if (!string.IsNullOrWhiteSpace(session.Description) && existing.Description != session.Description)
				{
					existing.Description = session.Description;
					hasChanges = true;
				}

				if (session.Duration.HasValue && existing.Duration != session.Duration.Value)
				{
					existing.Duration = session.Duration.Value;
					hasChanges = true;
				}

				if (!hasChanges)
				{
					_logger.LogInformation("Update TreatmentSession: No changes detected Id {Id}", session.Id);
					return true;
				}

				var updated = await _treatmentSessionRepository.UpdateEntity(existing);

				if (!updated)
				{
					_logger.LogError("Update TreatmentSession failed at repository level");
					return false;
				}

				_logger.LogInformation("Update TreatmentSession success Id {Id}", session.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update TreatmentSession exception Id {Id}", session.Id);
				return false;
			}
		}

		public async Task<bool> delete(DeleteTreatmentSession session)
		{
			try
			{
				_logger.LogInformation("Start deleting TreatmentSession");

				if (!session.Id.HasValue)
				{
					_logger.LogWarning("Delete TreatmentSession failed: Missing Id");
					return false;
				}

				var existingSession = await _treatmentSessionRepository.GetById(session.Id.Value);
				if (existingSession == null)
				{
					_logger.LogWarning("Delete TreatmentSession failed: Not found with Id {Id}", session.Id);
					return false;
				}

				var deleted = await _treatmentSessionRepository.DeleteEntitiesStatus(existingSession);

				if (!deleted)
				{
					_logger.LogError("Delete TreatmentSession failed at repository level: Id {Id}", session.Id);
					return false;
				}

				var remainingSessions = await _treatmentSessionRepository.FindByPredicate(x =>
					x.TreatmentPlanId == existingSession.TreatmentPlanId && !x.DeleteStatus);
				int remainingCount = remainingSessions.Count();
				var plan = await _treatmentPlanRepository.GetById(existingSession.TreatmentPlanId.Value);
				if (plan != null)
				{
					plan.TotalSessions = remainingCount;

					var planUpdated = await _treatmentPlanRepository.UpdateEntity(plan);
					if (!planUpdated)
					{
						_logger.LogError("Failed to update TotalSessions for TreatmentPlanId {TreatmentPlanId}", existingSession.TreatmentPlanId);
					}
					else
					{
						_logger.LogInformation("Updated TotalSessions to {TotalSessions} for TreatmentPlanId {TreatmentPlanId}",
							remainingCount, existingSession.TreatmentPlanId);
					}
				}

				// Re-order lại SessionNumber
				await ReOrderSessionNumbers(existingSession.TreatmentPlanId.Value);

				_logger.LogInformation("Delete TreatmentSession success: Id {Id}", session.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete TreatmentSession exception: Id {Id}", session.Id);
				return false;
			}
		}

		private async Task ReOrderSessionNumbers(int treatmentPlanId)
		{
			var sessions = (await _treatmentSessionRepository.FindByPredicate(x =>
					x.TreatmentPlanId == treatmentPlanId && !x.DeleteStatus))
				.OrderBy(x => x.SessionNumber)
				.ToList();

			int number = 1;

			foreach (var session in sessions)
			{
				session.SessionNumber = number++;
			}

			await _treatmentSessionRepository.UpdateRangeEntities(sessions);
		}

		public async Task<BaseDataCollection<TreatmentSessionEntity>> getlist(TreatmentSessionGet search)
		{
			try
			{
				Expression<Func<TreatmentSessionEntity, bool>> predicate = x => x.DeleteStatus != true;

				if (search.TreatmentPlanId.HasValue)
				{
					predicate = x => x.TreatmentPlanId == search.TreatmentPlanId.Value && x.DeleteStatus != true;
				}

				var data = await _treatmentSessionRepository.FindByPredicate(predicate);

				return new BaseDataCollection<TreatmentSessionEntity>(
					data.ToList(),
					data.Count,
					1,
					data.Count
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList TreatmentSession exception");

				return new BaseDataCollection<TreatmentSessionEntity>(
					null,
					0,
					1,
					0
				);
			}
		}
	}
}