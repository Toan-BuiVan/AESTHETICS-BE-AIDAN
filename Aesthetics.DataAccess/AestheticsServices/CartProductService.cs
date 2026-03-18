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
			if (!request.CartId.HasValue || (!request.ProductId.HasValue && !request.ServiceId.HasValue))
			{
				_logger.LogWarning("Create CartProduct failed: Missing required fields (CartId and either ProductId or ServiceId)");
				return false;
			}

			Expression<Func<CartProductEntity, bool>> predicate = x => x.CartId == request.CartId.Value && x.DeleteStatus != true;

			if (request.ProductId.HasValue)
			{
				predicate = predicate.And(x => x.ProductId == request.ProductId.Value && x.ServiceId == null);
			}
			else if (request.ServiceId.HasValue)
			{
				predicate = predicate.And(x => x.ServiceId == request.ServiceId.Value && x.ProductId == null);
			}

			var existingItems = await _cartProductRepository.FindByPredicate(predicate);
			if (existingItems.Any())
			{
				var existingItem = existingItems.First();
				existingItem.Quantity += 1;
				var updated = await _cartProductRepository.UpdateEntity(existingItem);
				if (!updated)
				{
					_logger.LogError("Create CartProduct failed during update quantity for existing item: CartId {CartId} ", request.CartId, request.ProductId, request.ServiceId);
					return false;
				}
				_logger.LogInformation("Create CartProduct success (quantity updated): CartId {CartId} ", request.CartId, request.ProductId, request.ServiceId);
				return true;
			}

			var newCartProduct = new CartProductEntity
			{
				CartId = request.CartId.Value,
				ProductId = request.ProductId,
				ServiceId = request.ServiceId,
				PriceAtAdd = request.PriceAtAdd ?? 0,
				Quantity = 1
			};
			await _cartProductRepository.CreateEntity(newCartProduct);
			_logger.LogInformation("Create CartProduct success (new item): CartId {CartId} ", request.CartId, request.ProductId, request.ServiceId);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating CartProduct: CartId {CartId} ", request.CartId, request.ProductId, request.ServiceId);
			return false;
		}
	}

	public async Task<bool> delete(DeleteCartProduct request)
	{
		try
		{
			_logger.LogInformation("Start deleting CartProduct");

			if (!request.Id.HasValue)
			{
				_logger.LogWarning("Delete CartProduct failed: Missing Id or (ProductId or ServiceId)");
				return false;
			}

			var existingCartProduct = await _cartProductRepository.GetById(request.Id.Value);
			if (existingCartProduct == null)
			{
				_logger.LogWarning("Delete CartProduct failed: Not found with Id {Id}", request.Id);
				return false;
			}

			var deleted = await _cartProductRepository.DeleteEntitiesStatus(existingCartProduct);
			if (!deleted)
			{
				_logger.LogError("Delete CartProduct failed at repository level: Id {Id} ", request.Id);
				return false;
			}

			_logger.LogInformation("Delete CartProduct success: Id {Id} ", request.Id);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Delete CartProduct exception: Id {Id} ", request.Id);
			return false;
		}
	}

	public async Task<BaseDataCollection<CartProductEntity>> getlist(GetCartProduct request)
	{
		try
		{
			if (!request.Id.HasValue)
			{
				_logger.LogWarning("GetList CartProduct failed: Missing CustomerId");
				return new BaseDataCollection<CartProductEntity>(null, 0, 1, int.MaxValue);
			}

			var cart = await _customerRepository.GetById(request.Id.Value);
			if (cart == null)
			{
				_logger.LogWarning("GetList CartProduct failed: Cart not found for CustomerId {CustomerId}", request.Id);
				return new BaseDataCollection<CartProductEntity>(null, 0, 1, int.MaxValue);
			}

			Expression<Func<CartProductEntity, bool>> predicate = x => x.CartId == cart.Id && x.DeleteStatus != true;

			var allMatching = await _cartProductRepository.FindByPredicate(predicate);
			var totalCount = allMatching.Count();
			var pagedData = allMatching
				.OrderByDescending(x => x.Id)
				.ToList(); 

			return new BaseDataCollection<CartProductEntity>(
				pagedData,
				totalCount,
				1,
				totalCount
			);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "GetList CartProduct exception for CustomerId {CustomerId}", request.Id);
			return new BaseDataCollection<CartProductEntity>(
				null,
				0,
				1,
				int.MaxValue
			);
		}
	}

	public async Task<bool> update(UpdateCartProduct request)
	{
		try
		{
			if (!request.Id.HasValue || !request.Quantity.HasValue)
			{
				_logger.LogWarning("Update CartProduct failed: Missing Id or Quantity");
				return false;
			}

			var existingCartProduct = await _cartProductRepository.GetById(request.Id.Value);
			if (existingCartProduct == null)
			{
				_logger.LogWarning("Update CartProduct failed: Not found with Id {Id}", request.Id);
				return false;
			}

			existingCartProduct.Quantity = request.Quantity.Value;

			var updated = await _cartProductRepository.UpdateEntity(existingCartProduct);
			if (!updated)
			{
				_logger.LogError("Update CartProduct failed at repository level: Id {Id}", request.Id);
				return false;
			}

			_logger.LogInformation("Update CartProduct success: Id {Id}", request.Id);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Update CartProduct exception: Id {Id}", request.Id);
			return false;
		}
	}
}