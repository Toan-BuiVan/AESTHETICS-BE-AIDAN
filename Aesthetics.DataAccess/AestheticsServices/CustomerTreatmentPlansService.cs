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

				var plan = await GetTreatmentPlan(request.TreatmentPlanId);
				if (plan == null)
					return false;

				var pricing = await GetUnitPrice(plan, request.IsFullPackage ?? false);
				if (!pricing.IsValid)
					return false;

				/*
					-- ChoDatLich: Chờ đặt lịch khám
					-- DangThucHien: đang chạy liệu trình
					-- HoanThanh: đã xong tất cả buổi
					-- TamDung: khách xin tạm dừng
					-- Huy: khách hủy giữa chừng
				*/

				var entity = new CustomerTreatmentPlanEntity
				{
					CustomerId = request.CustomerId,
					TreatmentPlanId = request.TreatmentPlanId,
					StartDate = request.StartDate ?? DateTime.UtcNow,
					CompletedSessions = 0,
					Status = "ChoDatLich",
					Notes = request.Notes,
					DeleteStatus = false
				};

				await _customerTreatmentPlansRepository.CreateEntity(entity);
				await CloneTreatmentSessions(entity.Id, request.TreatmentPlanId);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "CreateCustomerTreatment exception");
				return false;
			}
		}

		public async Task<bool> delete(DeleteCustomerTreatment dto)
		{
			try
			{
				if (dto == null || !dto.Id.HasValue)
				{
					_logger.LogWarning("DeleteCustomerTreatment: invalid payload");
					return false;
				}

				var existing = (await _customerTreatmentPlansRepository.FindByPredicate(x => x.Id == dto.Id.Value)).FirstOrDefault();
				if (existing == null)
				{
					_logger.LogWarning("DeleteCustomerTreatment: not found id={Id}", dto.Id);
					return false;
				}

				await _customerTreatmentPlansRepository.DeleteRangeEntitiesStatus(existing);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "DeleteCustomerTreatment: exception");
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

		public async Task<BaseDataCollection<CustomerTreatmentPlanEntity>> getlist(GetCustomerTreatment treatment)
		{
			try
			{
				Expression<Func<CustomerTreatmentPlanEntity, bool>> predicate = x => !x.DeleteStatus;

				if (treatment.CustomerId.HasValue)
				{
					predicate = predicate.And(x => x.CustomerId == treatment.CustomerId.Value);
				}

				var allMatching = await _customerTreatmentPlansRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count();

				var pagedData = allMatching
					.OrderByDescending(x => x.StartDate)
					.ThenBy(x => x.Id)
					.Skip((treatment.PageNo - 1) * treatment.PageSize)
					.Take(treatment.PageSize)
					.ToList();

				return new BaseDataCollection<CustomerTreatmentPlanEntity>(
					pagedData,
					totalCount,
					treatment.PageNo,
					treatment.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetCustomerTreatment list exception");
				return new BaseDataCollection<CustomerTreatmentPlanEntity>(
					null,
					0,
					treatment.PageNo,
					treatment.PageSize
				);
			}
		}

		private bool IsValidRequest(CreateCustomerTreatment request)
		{
			if (request == null || !request.CustomerId.HasValue)
			{
				_logger.LogWarning("Invalid request payload");
				return false;
			}

			if (!(request.IsFullPackage ?? false) && !request.TreatmentPlanId.HasValue)
			{
				_logger.LogWarning("TreatmentPlanId required when not full package");
				return false;
			}

			return true;
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

		private async Task CloneTreatmentSessions(int customerPlanId, int? treatmentPlanId)
		{
			if (!treatmentPlanId.HasValue)
				return;

			var templateSessions = await _treatmentSessionRepository
				.FindByPredicate(x => x.TreatmentPlanId == treatmentPlanId && !x.DeleteStatus);

			if (!templateSessions.Any())
				return;

			var sessions = templateSessions.Select(x => new CustomerTreatmentSessionEntity
			{
				CustomerTreatmentPlanId = customerPlanId,
				TreatmentSessionId = x.Id,
				Status = "ChoDatLich",
				DeleteStatus = false
			}).ToList();

			await _customerTreatmentSessionsRepository.CreateRangeEntities(sessions);
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
