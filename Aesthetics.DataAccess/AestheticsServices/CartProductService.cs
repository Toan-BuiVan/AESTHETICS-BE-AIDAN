using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using LinqKit;
using Microsoft.Extensions.Logging;

public class CartProductService : ICartProductService
{
	private readonly ICartProductRepository _cartProductRepository;
	private readonly ICartRepository _cartRepository;
	private readonly ILogger<CartProductService> _logger;
	private readonly ICustomerRepository _customerRepository;

	public CartProductService(
		ICartProductRepository cartProductRepository,
		ICartRepository cartRepository,
		ILogger<CartProductService> logger,
		ICustomerRepository customerRepository)
	{
		_cartProductRepository = cartProductRepository;
		_cartRepository = cartRepository;
		_logger = logger;
		_customerRepository = customerRepository;
	}

	public async Task<bool> create(CreateCartProduct request)
	{
		try
		{
			// ✅ Validate request
			if (!request.CustomerId.HasValue)
			{
				_logger.LogWarning("Create CartProduct failed: Missing CustomerId");
				return false;
			}

			if (!request.ProductId.HasValue)
			{
				_logger.LogWarning("Create CartProduct failed: Missing ProductId");
				return false;
			}

			// ✅ Query lấy CartId từ CustomerId
			var cart = (await _cartRepository.FindByPredicate(x =>
				x.CustomerId == request.CustomerId.Value && !x.DeleteStatus)).FirstOrDefault();

			if (cart == null)
			{
				_logger.LogWarning("Create CartProduct failed: Cart not found for CustomerId {CustomerId}", request.CustomerId);
				return false;
			}

			int cartId = cart.Id;

			// ✅ Build predicate để kiểm tra item tồn tại
			Expression<Func<CartProductEntity, bool>> predicate = x =>
				x.CartId == cartId &&
				x.ProductId == request.ProductId.Value &&
				!x.DeleteStatus;

			var existingItems = await _cartProductRepository.FindByPredicate(predicate);

			if (existingItems.Any())
			{
				// ✅ Item tồn tại → tăng quantity
				var existingItem = existingItems.First();
				existingItem.Quantity += request.Quantity ?? 1;

				var updated = await _cartProductRepository.UpdateEntity(existingItem);
				if (!updated)
				{
					_logger.LogError("Create CartProduct failed during update quantity: CartId {CartId}, ProductId {ProductId}",
						cartId, request.ProductId);
					return false;
				}

				_logger.LogInformation("Create CartProduct success (quantity updated): CartId {CartId}, ProductId {ProductId}, NewQuantity {Quantity}",
					cartId, request.ProductId, existingItem.Quantity);
				return true;
			}

			// ✅ Item không tồn tại → tạo mới
			var newCartProduct = new CartProductEntity
			{
				CartId = cartId,
				ProductId = request.ProductId,
				PriceAtAdd = request.PriceAtAdd ?? 0,
				Quantity = request.Quantity ?? 1,
				CreateDate = DateTime.UtcNow,
				DeleteStatus = false
			};

			var created = await _cartProductRepository.CreateEntity(newCartProduct);
			if (!created)
			{
				_logger.LogError("Create CartProduct failed: Cannot create new item for CartId {CartId}", cartId);
				return false;
			}

			_logger.LogInformation("Create CartProduct success (new item): CartId {CartId}, ProductId {ProductId}, Quantity {Quantity}",
				cartId, request.ProductId, newCartProduct.Quantity);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating CartProduct");
			return false;
		}
	}

	public async Task<bool> delete(DeleteCartProduct request)
	{
		try
		{
			_logger.LogInformation("Start deleting CartProduct");

			// ✅ Validate
			if (!request.CartProductId.HasValue)
			{
				_logger.LogWarning("Delete CartProduct failed: Missing CartProductId");
				return false;
			}

			var existingCartProduct = await _cartProductRepository.GetById(request.CartProductId.Value);
			if (existingCartProduct == null)
			{
				_logger.LogWarning("Delete CartProduct failed: Not found with Id {Id}", request.CartProductId);
				return false;
			}

			var deleted = await _cartProductRepository.DeleteEntitiesStatus(existingCartProduct);
			if (!deleted)
			{
				_logger.LogError("Delete CartProduct failed at repository level: Id {Id}", request.CartProductId);
				return false;
			}

			_logger.LogInformation("Delete CartProduct success: Id {Id}", request.CartProductId);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Delete CartProduct exception: Id {Id}", request.CartProductId);
			return false;
		}
	}

	public async Task<BaseDataCollection<CartProductResponseModel>> getlist(GetCartProduct request)
	{
		try
		{
			// ✅ Validate
			if (!request.CustomerId.HasValue)
			{
				_logger.LogWarning("GetList CartProduct failed: Missing CustomerId");
				return new BaseDataCollection<CartProductResponseModel>(null, 0, request.PageNo, request.PageSize);
			}

			// ✅ Query lấy CartId từ CustomerId (gọi repository)
			var carts = await _cartRepository.FindByPredicate(x =>
				x.CustomerId == request.CustomerId.Value && !x.DeleteStatus);

			var cart = carts.FirstOrDefault();

			if (cart == null)
			{
				_logger.LogWarning("GetList CartProduct failed: Cart not found for CustomerId {CustomerId}", request.CustomerId);
				return new BaseDataCollection<CartProductResponseModel>(null, 0, request.PageNo, request.PageSize);
			}

			// ✅ Lấy CartProduct với Product được Include (repository xử lý)
			var allMatching = await _cartProductRepository.GetCartProductsByCartIdAsync(cart.Id);

			var totalCount = allMatching.Count;

			// ✅ Mapping sang CartProductResponseModel + Phân trang
			var pagedData = allMatching
				.Skip((request.PageNo - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(cp => new CartProductResponseModel
				{
					Id = cp.Id,
					CartId = cp.CartId,
					ProductId = cp.ProductId,
					Quantity = cp.Quantity,
					PriceAtAdd = cp.PriceAtAdd,
					CreateDate = cp.CreateDate,
					// Thông tin sản phẩm
					ProductName = cp.Product?.ProductName,
					ProductImages = cp.Product?.ProductImages,
					Description = cp.Product?.Description,
					SellingPrice = cp.Product?.SellingPrice,
					Unit = cp.Product?.Unit
				})
				.ToList();

			_logger.LogInformation("GetList CartProduct success: CustomerId {CustomerId}, Total {Total}, Page {PageNo}/{PageSize}",
				request.CustomerId, totalCount, request.PageNo, request.PageSize);

			return new BaseDataCollection<CartProductResponseModel>(
				pagedData,
				totalCount,
				request.PageNo,
				request.PageSize
			);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "GetList CartProduct exception for CustomerId {CustomerId}", request.CustomerId);
			return new BaseDataCollection<CartProductResponseModel>(
				null,
				0,
				request.PageNo,
				request.PageSize
			);
		}
	}

	public async Task<bool> update(UpdateCartProduct request)
	{
		try
		{
			// ✅ Validate
			if (!request.CartProductId.HasValue)
			{
				_logger.LogWarning("Update CartProduct failed: Missing CartProductId");
				return false;
			}

			if (!request.Quantity.HasValue)
			{
				_logger.LogWarning("Update CartProduct failed: Missing Quantity");
				return false;
			}

			var existingCartProduct = await _cartProductRepository.GetById(request.CartProductId.Value);
			if (existingCartProduct == null)
			{
				_logger.LogWarning("Update CartProduct failed: Not found with Id {Id}", request.CartProductId);
				return false;
			}

			// ✅ Cập nhật Quantity
			existingCartProduct.Quantity = request.Quantity.Value;

			var updated = await _cartProductRepository.UpdateEntity(existingCartProduct);
			if (!updated)
			{
				_logger.LogError("Update CartProduct failed at repository level: Id {Id}", request.CartProductId);
				return false;
			}

			_logger.LogInformation("Update CartProduct success: Id {Id}, NewQuantity {Quantity}",
				request.CartProductId, existingCartProduct.Quantity);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Update CartProduct exception: Id {Id}", request.CartProductId);
			return false;
		}
	}
}