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
	public class SessionProductService : ISessionProductService
	{
		private readonly ILogger<SessionProductService> _logger;
		private readonly ISessionProductRepository _sessionProductRepository;
		private readonly IProductRepository _productRepository;

		public SessionProductService(ILogger<SessionProductService> logger
			, ISessionProductRepository sessionProductRepository)
		{
			_logger = logger;
			_sessionProductRepository = sessionProductRepository;
		}

		/// <summary>
		/// Tạo mới SessionProduct - thêm sản phẩm vào một buổi treatment
		/// LUỒNG: Validate input → Tạo entity → Lưu database → Log kết quả
		/// </summary>
		public async Task<bool> create(CreateSessionProduct sessionProduct)
		{
			try
			{
				// BƯỚC 1: Validate input
				if (!IsValidCreateRequest(sessionProduct))
				{
					return false;
				}

				// BƯỚC 2: Kiểm tra tồn kho trước khi sử dụng
				var product = await _productRepository.GetById(sessionProduct.ProductId.Value);
				if (product == null)
				{
					_logger.LogWarning("Product not found: ProductId {ProductId}", sessionProduct.ProductId);
					return false;
				}

				int quantityToUse = sessionProduct.QuantityUsed ?? 1;
				if (product.Quantity < quantityToUse)
				{
					_logger.LogWarning("Insufficient stock: ProductId {ProductId}, Available {Available}, Required {Required}",
						sessionProduct.ProductId, product.Quantity, quantityToUse);
					return false;
				}

				// BƯỚC 3: Tạo SessionProduct entity
				var entity = new SessionProductEntity
				{
					TreatmentSessionId = sessionProduct.TreatmentSessionId.Value,
					ProductId = sessionProduct.ProductId.Value,
					QuantityUsed = quantityToUse,
					ServiceId = sessionProduct.ServiceId,
					DeleteStatus = false
				};

				// BƯỚC 4: Lưu SessionProduct
				var created = await _sessionProductRepository.CreateEntity(entity);
				if (!created)
				{
					_logger.LogError("Create SessionProduct failed at repository level");
					return false;
				}

				// BƯỚC 5: TRỪ TỒN KHO sản phẩm
				await UpdateProductStock(sessionProduct.ProductId.Value, -quantityToUse, $"Used in TreatmentSession {sessionProduct.TreatmentSessionId}");

				_logger.LogInformation("Create SessionProduct success and updated stock: ProductId {ProductId}, Used {Used}",
					sessionProduct.ProductId, quantityToUse);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create SessionProduct exception");
				return false;
			}
		}

		/// <summary>
		/// Cập nhật tồn kho sản phẩm và ghi log lịch sử
		/// </summary>
		private async Task UpdateProductStock(int productId, int quantityChange, string reason)
		{
			try
			{
				var product = await _productRepository.GetById(productId);
				if (product == null) return;

				// Cập nhật số lượng tồn kho
				int oldQuantity = product.Quantity;
				product.Quantity += quantityChange; // quantityChange có thể âm (trừ) hoặc dương (cộng)

				if (product.Quantity < 0)
				{
					_logger.LogWarning("Stock became negative after update: ProductId {ProductId}, OldStock {OldStock}, Change {Change}",
						productId, oldQuantity, quantityChange);
					product.Quantity = 0; // Không cho phép âm
				}

				// Lưu thay đổi
				await _productRepository.UpdateEntity(product);

				// Ghi log lịch sử thay đổi tồn kho (nếu có bảng StockHistory)
				await LogStockChange(productId, oldQuantity, product.Quantity, quantityChange, reason);

				// Cảnh báo nếu tồn kho dưới ngưỡng tối thiểu
				if (product.Quantity <= product.MinimumStock)
				{
					_logger.LogWarning("Stock below minimum: ProductId {ProductId}, Current {Current}, Minimum {Minimum}",
						productId, product.Quantity, product.MinimumStock);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to update stock for ProductId {ProductId}", productId);
				throw; // Re-throw để rollback transaction
			}
		}

		/// <summary>
		/// Ghi log lịch sử thay đổi tồn kho
		/// </summary>
		private async Task LogStockChange(int productId, int oldQuantity, int newQuantity, int change, string reason)
		{
			// Có thể tạo bảng StockHistories để theo dõi lịch sử thay đổi tồn kho
			// Hoặc log vào hệ thống logging
			_logger.LogInformation("Stock changed: ProductId {ProductId}, From {From} to {To}, Change {Change}, Reason: {Reason}",
				productId, oldQuantity, newQuantity, change, reason);
		}


		/// <summary>
		/// Cập nhật SessionProduct - chỉ cho phép sửa số lượng sử dụng
		/// LUỒNG: Validate input → Tìm record → Kiểm tra thay đổi → Cập nhật → Log kết quả
		/// </summary>
		public async Task<bool> update(UpdateSessionProduct sessionProduct)
		{
			try
			{
				// BƯỚC 1: Validate input
				if (sessionProduct.Id <= 0)
				{
					_logger.LogWarning("Update SessionProduct failed: Invalid Id {Id}", sessionProduct.Id);
					return false;
				}

				// BƯỚC 2: Tìm record hiện tại
				var existing = await _sessionProductRepository.GetById(sessionProduct.Id);
				if (existing == null)
				{
					_logger.LogWarning("Update SessionProduct failed: Not found Id {Id}", sessionProduct.Id);
					return false;
				}

				// BƯỚC 3: Kiểm tra có thay đổi gì không
				bool hasChanges = false;

				if (sessionProduct.QuantityUsed.HasValue && existing.QuantityUsed != sessionProduct.QuantityUsed.Value)
				{
					existing.QuantityUsed = sessionProduct.QuantityUsed.Value;
					hasChanges = true;
				}

				// BƯỚC 4: Nếu không có thay đổi thì return luôn
				if (!hasChanges)
				{
					_logger.LogInformation("Update SessionProduct: No changes detected for Id {Id}", sessionProduct.Id);
					return true;
				}

				// BƯỚC 5: Cập nhật database
				var updated = await _sessionProductRepository.UpdateEntity(existing);

				if (!updated)
				{
					_logger.LogError("Update SessionProduct failed at repository level: Id {Id}", sessionProduct.Id);
					return false;
				}

				_logger.LogInformation("Update SessionProduct success: Id {Id}", sessionProduct.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update SessionProduct exception: Id {Id}", sessionProduct.Id);
				return false;
			}
		}

		/// <summary>
		/// Xóa SessionProduct (soft delete) - đánh dấu DeleteStatus = true
		/// LUỒNG: Validate input → Tìm record → Soft delete → Log kết quả
		/// </summary>
		public async Task<bool> delete(DeleteSessionProduct sessionProduct)
		{
			try
			{
				// BƯỚC 1: Validate input
				if (sessionProduct.Id <= 0)
				{
					_logger.LogWarning("Delete SessionProduct failed: Invalid Id {Id}", sessionProduct.Id);
					return false;
				}

				// BƯỚC 2: Tìm record hiện tại
				var existing = await _sessionProductRepository.GetById(sessionProduct.Id);
				if (existing == null)
				{
					_logger.LogWarning("Delete SessionProduct failed: Not found Id {Id}", sessionProduct.Id);
					return false;
				}

				// BƯỚC 3: Soft delete - đặt DeleteStatus = true
				var deleted = await _sessionProductRepository.DeleteEntitiesStatus(existing);

				if (!deleted)
				{
					_logger.LogError("Delete SessionProduct failed at repository level: Id {Id}", sessionProduct.Id);
					return false;
				}

				_logger.LogInformation("Delete SessionProduct success: Id {Id}", sessionProduct.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete SessionProduct exception: Id {Id}", sessionProduct.Id);
				return false;
			}
		}

		/// <summary>
		/// Lấy danh sách SessionProduct với phân trang và lọc
		/// LUỒNG: Tạo predicate → Lọc data → Phân trang → Sắp xếp → Trả về kết quả
		/// </summary>
		public async Task<BaseDataCollection<SessionProductEntity>> getlist(SessionProductGet sessionProduct)
		{
			try
			{
				// BƯỚC 1: Tạo điều kiện lọc cơ bản - chỉ lấy record chưa xóa
				Expression<Func<SessionProductEntity, bool>> predicate = x => !x.DeleteStatus;

				// BƯỚC 2: Thêm điều kiện lọc theo ServiceId nếu có
				if (sessionProduct.ServiceId.HasValue)
				{
					predicate = predicate.And(x => x.ServiceId == sessionProduct.ServiceId.Value);
				}

				// BƯỚC 3: Thêm điều kiện lọc theo TreatmentSessionId nếu có
				if (sessionProduct.TreatmentSessionId.HasValue)
				{
					predicate = predicate.And(x => x.TreatmentSessionId == sessionProduct.TreatmentSessionId.Value);
				}

				// BƯỚC 4: Lấy tất cả data thỏa mãn điều kiện
				var allMatching = await _sessionProductRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count();

				// BƯỚC 5: Áp dụng phân trang và sắp xếp
				var pagedData = allMatching
					.OrderBy(x => x.TreatmentSessionId)  // Sắp xếp theo buổi
					.ThenBy(x => x.ProductId)            // Rồi theo sản phẩm
					.Skip((sessionProduct.PageNo - 1) * sessionProduct.PageSize)  // Bỏ qua records của trang trước
					.Take(sessionProduct.PageSize)       // Lấy records của trang hiện tại
					.ToList();

				// BƯỚC 6: Tạo kết quả trả về với thông tin phân trang
				return new BaseDataCollection<SessionProductEntity>(
					pagedData,
					totalCount,
					sessionProduct.PageNo,
					sessionProduct.PageSize
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList SessionProduct exception");
				return new BaseDataCollection<SessionProductEntity>(
					null,
					0,
					sessionProduct.PageNo,
					sessionProduct.PageSize
				);
			}
		}

		/// <summary>
		/// Validate dữ liệu input khi tạo mới
		/// </summary>
		private bool IsValidCreateRequest(CreateSessionProduct sessionProduct)
		{
			if (sessionProduct == null)
			{
				_logger.LogWarning("Create SessionProduct failed: Request is null");
				return false;
			}

			if (!sessionProduct.TreatmentSessionId.HasValue)
			{
				_logger.LogWarning("Create SessionProduct failed: Missing TreatmentSessionId");
				return false;
			}

			if (!sessionProduct.ProductId.HasValue)
			{
				_logger.LogWarning("Create SessionProduct failed: Missing ProductId");
				return false;
			}

			if (sessionProduct.QuantityUsed.HasValue && sessionProduct.QuantityUsed.Value <= 0)
			{
				_logger.LogWarning("Create SessionProduct failed: Invalid QuantityUsed {QuantityUsed}", sessionProduct.QuantityUsed);
				return false;
			}

			return true;
		}
	}
}
