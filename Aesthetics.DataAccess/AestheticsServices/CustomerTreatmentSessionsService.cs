using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
	public class CustomerTreatmentSessionsService : ICustomerTreatmentSessionsService
	{
		private readonly ILogger<CustomerTreatmentSessionsService> _logger;
		private readonly ICustomerTreatmentSessionsRepository _customerTreatmentSessionsRepository;
		private readonly ICustomerTreatmentPlansRepository _customerTreatmentPlansRepository;
		private readonly ITreatmentSessionRepository _treatmentSessionRepository;
		private readonly ISessionProductRepository _sessionProductRepository;
		private readonly IProductRepository _productRepository;
		private readonly IInventoryAlertService _inventoryAlertService;

		public CustomerTreatmentSessionsService(
			ILogger<CustomerTreatmentSessionsService> logger,
			ICustomerTreatmentSessionsRepository customerTreatmentSessionsRepository,
			ICustomerTreatmentPlansRepository customerTreatmentPlansRepository,
			ITreatmentSessionRepository treatmentSessionRepository,
			ISessionProductRepository sessionProductRepository,
			IProductRepository productRepository,
			IInventoryAlertService inventoryAlertService)
		{
			_logger = logger;
			_customerTreatmentSessionsRepository = customerTreatmentSessionsRepository;
			_customerTreatmentPlansRepository = customerTreatmentPlansRepository;
			_treatmentSessionRepository = treatmentSessionRepository;
			_sessionProductRepository = sessionProductRepository;
			_productRepository = productRepository;
			_inventoryAlertService = inventoryAlertService;
		}

		public async Task<bool> update(UpdateCustomerTreatmentSessions requestCustomer)
		{
			try
			{
				// BƯỚC 1: Validate input
				if (!requestCustomer.Id.HasValue)
				{
					_logger.LogWarning("UpdateCustomerTreatmentSession: Missing Id");
					return false;
				}

				// BƯỚC 2: Tìm session hiện tại
				var existingSession = await _customerTreatmentSessionsRepository.GetById(requestCustomer.Id.Value);
				if (existingSession == null)
				{
					_logger.LogWarning("UpdateCustomerTreatmentSession: Not found Id {Id}", requestCustomer.Id);
					return false;
				}

				// BƯỚC 3: Kiểm tra thay đổi
				bool hasChanges = false;
				string oldStatus = existingSession.Status;

				if (!string.IsNullOrWhiteSpace(requestCustomer.Status) && existingSession.Status != requestCustomer.Status)
				{
					existingSession.Status = requestCustomer.Status;
					hasChanges = true;
				}

				if (!hasChanges)
				{
					_logger.LogInformation("UpdateCustomerTreatmentSession: No changes detected for Id {Id}", requestCustomer.Id);
					return true;
				}

				// BƯỚC 4: Cập nhật session
				var updated = await _customerTreatmentSessionsRepository.UpdateEntity(existingSession);
				if (!updated)
				{
					_logger.LogError("UpdateCustomerTreatmentSession: Failed at repository level for Id {Id}", requestCustomer.Id);
					return false;
				}

				// BƯỚC 4.5: CẬP NHẬT TỒN KHO NGAY KHI SESSION HOÀN THÀNH
				if (requestCustomer.Status == "HoanThanh" && oldStatus != "HoanThanh")
				{
					await UpdateProductInventoryForSession(existingSession.Id, existingSession.TreatmentSessionId);
				}

				// BƯỚC 5: Cập nhật status của CustomerTreatmentPlan dựa trên sessions
				if (existingSession.CustomerTreatmentPlanId.HasValue)
				{
					await UpdateCustomerTreatmentPlanStatus(existingSession.CustomerTreatmentPlanId.Value, oldStatus, requestCustomer.Status);
				}

				_logger.LogInformation("UpdateCustomerTreatmentSession: Success for Id {Id}, Status changed from {OldStatus} to {NewStatus}",
					requestCustomer.Id, oldStatus, requestCustomer.Status);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UpdateCustomerTreatmentSession: Exception for Id {Id}", requestCustomer.Id);
				return false;
			}
		}


		/// <summary>
		/// Cập nhật tồn kho sản phẩm ngay khi 1 session hoàn thành
		/// </summary>
		private async Task UpdateProductInventoryForSession(int customerSessionId, int? treatmentSessionId)
		{
			try
			{
				if (!treatmentSessionId.HasValue)
				{
					_logger.LogWarning("UpdateProductInventoryForSession: No TreatmentSessionId for CustomerSession {Id}", customerSessionId);
					return;
				}

				// Lấy tất cả session products của session này
				var sessionProducts = await _sessionProductRepository
					.FindByPredicate(x => x.TreatmentSessionId == treatmentSessionId.Value && !x.DeleteStatus);

				if (!sessionProducts.Any())
				{
					_logger.LogInformation("UpdateProductInventoryForSession: No session products found for TreatmentSession {Id}", treatmentSessionId);
					return;
				}

				// Cập nhật tồn kho cho từng sản phẩm
				foreach (var sessionProduct in sessionProducts)
				{
					if (sessionProduct.ProductId.HasValue && sessionProduct.QuantityUsed.HasValue)
					{
						await UpdateProductStock(sessionProduct.ProductId.Value, sessionProduct.QuantityUsed.Value,
							$"Session completed - CustomerSession {customerSessionId}");
					}
				}

				_logger.LogInformation("UpdateProductInventoryForSession: Updated inventory for {Count} products from CustomerSession {Id}",
					sessionProducts.Count(), customerSessionId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UpdateProductInventoryForSession: Exception for CustomerSession {Id}", customerSessionId);
			}
		}


		/// <summary>
		/// Cập nhật status của CustomerTreatmentPlan dựa trên status của các sessions
		/// </summary>
		private async Task UpdateCustomerTreatmentPlanStatus(int customerTreatmentPlanId, string oldSessionStatus, string newSessionStatus)
		{
			try
			{
				// Lấy thông tin plan hiện tại
				var customerPlan = await _customerTreatmentPlansRepository.GetById(customerTreatmentPlanId);
				if (customerPlan == null)
				{
					_logger.LogWarning("UpdateCustomerTreatmentPlanStatus: CustomerTreatmentPlan not found {Id}", customerTreatmentPlanId);
					return;
				}

				// Lấy tất cả sessions của plan này
				var allSessions = await _customerTreatmentSessionsRepository
					.FindByPredicate(x => x.CustomerTreatmentPlanId == customerTreatmentPlanId && !x.DeleteStatus);

				if (!allSessions.Any())
				{
					_logger.LogWarning("UpdateCustomerTreatmentPlanStatus: No sessions found for CustomerTreatmentPlan {Id}", customerTreatmentPlanId);
					return;
				}

				// Xác định status mới cho plan
				string newPlanStatus = DeterminePlanStatus(allSessions.ToList());
				string oldPlanStatus = customerPlan.Status;

				if (newPlanStatus != oldPlanStatus)
				{
					customerPlan.Status = newPlanStatus;

					var planUpdated = await _customerTreatmentPlansRepository.UpdateEntity(customerPlan);
					if (!planUpdated)
					{
						_logger.LogError("UpdateCustomerTreatmentPlanStatus: Failed to update plan status for Id {Id}", customerTreatmentPlanId);
						return;
					}

					_logger.LogInformation("UpdateCustomerTreatmentPlanStatus: Plan {PlanId} status changed from {OldStatus} to {NewStatus}, CompletedSessions: {CompletedSessions}",
						customerTreatmentPlanId, oldPlanStatus, newPlanStatus);

					// NOTE: Inventory đã được cập nhật ngay khi từng session hoàn thành
					// Không cần đợi đến khi cả plan hoàn thành
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UpdateCustomerTreatmentPlanStatus: Exception for CustomerTreatmentPlan {Id}", customerTreatmentPlanId);
			}
		}


		/// <summary>
		/// Xác định status mới cho CustomerTreatmentPlan dựa trên status của các sessions
		/// </summary>
		private string DeterminePlanStatus(List<CustomerTreatmentSessionEntity> sessions)
		{
			if (!sessions.Any())
				return "ChoDatLich";

			// Đếm số sessions theo từng status
			int totalSessions = sessions.Count;
			int completedSessions = sessions.Count(s => s.Status == "HoanThanh");
			int missedSessions = sessions.Count(s => s.Status == "BoLo");
			int activeSessions = sessions.Count(s => s.Status == "ChuaThucHien" ||
												 s.Status == "DaDatLich" ||
												 s.Status == "DangThucHien");

			// LOGIC QUYẾT ĐỊNH STATUS:

			// 1. Nếu tất cả sessions đều hoàn thành → Plan hoàn thành
			if (completedSessions == totalSessions)
			{
				return "HoanThanh";
			}

			// 2. Nếu có ít nhất 1 session hoàn thành hoặc đang active → Plan đang thực hiện
			if (completedSessions > 0 || activeSessions > 0)
			{
				return "DangThucHien";
			}

			// 3. Nếu tất cả sessions đều bỏ lỡ → Plan tạm dừng (cần can thiệp)
			if (missedSessions == totalSessions)
			{
				return "TamDung";
			}

			// 4. Trường hợp còn lại → Plan đang chờ đặt lịch
			return "ChoDatLich";
		}

		/// <summary>
		/// Cập nhật số lượng tồn kho sản phẩm
		/// </summary>
		private async Task UpdateProductStock(int productId, int quantityUsed, string reason)
		{
			try
			{
				var product = await _productRepository.GetById(productId);
				if (product == null)
				{
					_logger.LogWarning("UpdateProductStock: Product not found {ProductId}", productId);
					return;
				}

				int oldQuantity = product.Quantity;
				int newQuantity = oldQuantity - quantityUsed;

				// Đảm bảo số lượng không âm
				if (newQuantity < 0)
				{
					_logger.LogWarning("UpdateProductStock: Quantity would be negative for ProductId {ProductId}, setting to 0. Old: {Old}, Used: {Used}",
						productId, oldQuantity, quantityUsed);
					newQuantity = 0;
				}

				product.Quantity = newQuantity;

				var updated = await _productRepository.UpdateEntity(product);
				if (!updated)
				{
					_logger.LogError("UpdateProductStock: Failed to update ProductId {ProductId}", productId);
					return;
				}

				_logger.LogInformation("UpdateProductStock: ProductId {ProductId} updated from {Old} to {New} (Used: {Used}) - {Reason}",
					productId, oldQuantity, newQuantity, quantityUsed, reason);

				// TẠO CẢNH BÁO KHI TỒN KHO DƯỚI NGƯỠNG TỐI THIỂU
				if (product.Quantity <= product.MinimumStock)
				{
					_logger.LogWarning("UpdateProductStock: Stock below minimum for ProductId {ProductId}, Current: {Current}, Minimum: {Minimum}",
						productId, product.Quantity, product.MinimumStock);

					// Tạo inventory alert
					await _inventoryAlertService.CreateLowStockAlert(productId, product.Quantity, product.MinimumStock);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "UpdateProductStock: Exception for ProductId {ProductId}", productId);
			}
		}

		public async Task<bool> delete(DeleteCustomerTreatmentSessions requestCustomer)
		{
			try
			{
				// 1. Validate input
				if (!requestCustomer.Id.HasValue)
				{
					_logger.LogWarning("DeleteCustomerTreatmentSession: Missing Id");
					return false;
				}

				// 2. Find the session
				var existingSession = await _customerTreatmentSessionsRepository.GetById(requestCustomer.Id.Value);
				if (existingSession == null || existingSession.DeleteStatus)
				{
					_logger.LogWarning("DeleteCustomerTreatmentSession: Not found or already deleted Id {Id}", requestCustomer.Id);
					return false;
				}

				// 3. Soft delete the session
				var deleted = await _customerTreatmentSessionsRepository.DeleteEntitiesStatus(existingSession);
				if (!deleted)
				{
					_logger.LogError("DeleteCustomerTreatmentSession: Failed at repository level for Id {Id}", requestCustomer.Id);
					return false;
				}

				// 4. Update parent plan status if needed
				if (existingSession.CustomerTreatmentPlanId.HasValue)
				{
					await UpdateCustomerTreatmentPlanStatus(
						existingSession.CustomerTreatmentPlanId.Value,
						existingSession.Status,
						null
					);
				}

				_logger.LogInformation("DeleteCustomerTreatmentSession: Success for Id {Id}", requestCustomer.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "DeleteCustomerTreatmentSession: Exception for Id {Id}", requestCustomer.Id);
				return false;
			}
		}

	}
}
