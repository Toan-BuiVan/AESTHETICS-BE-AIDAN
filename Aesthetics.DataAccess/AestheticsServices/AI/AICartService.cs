using Aesthetics.Data.AestheticsInterfaces.AI;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.ResponseModel.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices.AI
{
	public class AICartService : IAICartService
	{
		private readonly ILogger<AICartService> _logger;
		private readonly ICartProductRepository _cartProductRepository;
		private readonly ICartRepository _cartRepository;
		private readonly IProductRepository _productRepository;
		private readonly ICustomerRepository _customerRepository;

		public AICartService(
			ILogger<AICartService> logger,
			ICartProductRepository cartProductRepository,
			ICartRepository cartRepository,
			IProductRepository productRepository,
			ICustomerRepository customerRepository)
		{
			_logger = logger;
			_cartProductRepository = cartProductRepository;
			_cartRepository = cartRepository;
			_productRepository = productRepository;
			_customerRepository = customerRepository;
		}

		/// <summary>Bài 16: Thêm sản phẩm vào giỏ hàng</summary>
		public async Task<AICartActionResponse> AddProductToCartAsync(int customerId, int productId, int quantity = 1)
		{
			try
			{
				_logger.LogInformation("ADD_PRODUCT_TO_CART: customerId={CustomerId}, productId={ProductId}, quantity={Quantity}", 
					customerId, productId, quantity);

				var response = new AICartActionResponse();

				// Kiểm tra khách hàng
				var customer = await _customerRepository.GetById(customerId);
				if (customer == null || customer.DeleteStatus)
				{
					response.Success = false;
					response.Message = "Khách hàng không tồn tại";
					return response;
				}

				// Kiểm tra sản phẩm
				var product = await _productRepository.GetById(productId);
				if (product == null || product.DeleteStatus)
				{
					response.Success = false;
					response.Message = "Sản phẩm không tồn tại";
					return response;
				}

				// Kiểm tra số lượng tồn kho
				if (product.Quantity < quantity)
				{
					response.Success = false;
					response.Message = $"Số lượng tồn kho không đủ. Chỉ còn {product.Quantity} sản phẩm";
					return response;
				}

				// Lấy hoặc tạo giỏ hàng của khách hàng
				var cart = (await _cartRepository.FindByPredicate(x =>
					x.	CustomerId == customerId &&
					!x.DeleteStatus)).FirstOrDefault();

				if (cart == null)
				{
					// Tạo giỏ hàng mới
					cart = new CartEntity
					{
						CustomerId = customerId,
						CreationDate = DateTime.UtcNow,
						DeleteStatus = false
					};
					var cartCreated = await _cartRepository.CreateEntity(cart);
					if (!cartCreated)
					{
						response.Success = false;
						response.Message = "Không thể tạo giỏ hàng";
						return response;
					}
				}

				// Kiểm tra sản phẩm đã có trong giỏ chưa
				var existingCartProduct = (await _cartProductRepository.FindByPredicate(x =>
					x.CartId == cart.Id &&
					x.ProductId == productId &&
					!x.DeleteStatus)).FirstOrDefault();

				if (existingCartProduct != null)
				{
					// Cập nhật số lượng
					existingCartProduct.Quantity += quantity;
					var updated = await _cartProductRepository.UpdateEntity(existingCartProduct);
					if (!updated)
					{
						response.Success = false;
						response.Message = "Không thể cập nhật số lượng sản phẩm";
						return response;
					}
					response.CartProductId = existingCartProduct.Id;
					response.Message = $"Cập nhật số lượng sản phẩm thành {existingCartProduct.Quantity}";
				}
				else
				{
					// Tạo mới
					var newCartProduct = new CartProductEntity
					{
						CartId = cart.Id,
						ProductId = productId,
						Quantity = quantity,
						PriceAtAdd = product.SellingPrice,
						CreateDate = DateTime.UtcNow,
						DeleteStatus = false
					};
					var created = await _cartProductRepository.CreateEntity(newCartProduct);
					if (!created)
					{
						response.Success = false;
						response.Message = "Không thể thêm sản phẩm vào giỏ hàng";
						return response;
					}
					response.CartProductId = newCartProduct.Id;
					response.Message = $"Đã thêm {quantity} {product.Unit} sản phẩm {product.ProductName} vào giỏ";
				}

				response.Success = true;
				response.ProductName = product.ProductName;
				response.ProductId = productId;
				response.Quantity = quantity;
				response.Price = product.SellingPrice ?? 0;

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ADD_PRODUCT_TO_CART_ERROR: Exception occurred");
				return new AICartActionResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}"
				};
			}
		}

		/// <summary>Bài 17: Xóa sản phẩm khỏi giỏ hàng</summary>
		public async Task<AICartActionResponse> RemoveProductFromCartAsync(int customerId, int productId)
		{
			try
			{
				_logger.LogInformation("REMOVE_PRODUCT_FROM_CART: customerId={CustomerId}, productId={ProductId}", 
					customerId, productId);

				var response = new AICartActionResponse();

				// Kiểm tra khách hàng
				var customer = await _customerRepository.GetById(customerId);
				if (customer == null || customer.DeleteStatus)
				{
					response.Success = false;
					response.Message = "Khách hàng không tồn tại";
					return response;
				}

				// Kiểm tra sản phẩm
				var product = await _productRepository.GetById(productId);
				if (product == null || product.DeleteStatus)
				{
					response.Success = false;
					response.Message = "Sản phẩm không tồn tại";
					return response;
				}

				// Lấy giỏ hàng của khách hàng
				var cart = (await _cartRepository.FindByPredicate(x =>
					x.CustomerId == customerId &&
					!x.DeleteStatus)).FirstOrDefault();

				if (cart == null)
				{
					response.Success = false;
					response.Message = "Khách hàng không có giỏ hàng";
					return response;
				}

				// Tìm và xóa sản phẩm khỏi giỏ hàng
				var cartProduct = (await _cartProductRepository.FindByPredicate(x =>
					x.CartId == cart.Id &&
					x.ProductId == productId &&
					!x.DeleteStatus)).FirstOrDefault();

				if (cartProduct == null)
				{
					response.Success = false;
					response.Message = "Sản phẩm không có trong giỏ hàng";
					return response;
				}

				cartProduct.DeleteStatus = true;
				var updated = await _cartProductRepository.UpdateEntity(cartProduct);

				if (!updated)
				{
					response.Success = false;
					response.Message = "Không thể xóa sản phẩm khỏi giỏ hàng";
					return response;
				}

				response.Success = true;
				response.ProductName = product.ProductName;
				response.ProductId = productId;
				response.Message = $"Đã xóa {product.ProductName} khỏi giỏ hàng";

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "REMOVE_PRODUCT_FROM_CART_ERROR: Exception occurred");
				return new AICartActionResponse
				{
					Success = false,
					Message = $"Lỗi: {ex.Message}"
				};
			}
		}
	}
}