using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Data.RepositoryServices.Common;
using Aesthetics.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.RepositoryServices
{
    public class CartProductRepository : CommonRepository<CartProductEntity>, ICartProductRepository
	{
		public CartProductRepository(ILogger<CommonRepository<CartProductEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}
		public async Task<ICollection<CartProductEntity>> GetCartProductsByCartIdAsync(int cartId)
		{
			try
			{
				var cartProducts = await _dbContext.Set<CartProductEntity>()
					.AsNoTracking()
					.Where(cp => cp.CartId == cartId && !cp.DeleteStatus)
					.Include(cp => cp.Product)
					.OrderByDescending(cp => cp.CreateDate)
					.ToListAsync();

				_logger.LogInformation("GetCartProductsByCartIdAsync - CartId: {CartId} - Count: {Count}", cartId, cartProducts.Count);
				return cartProducts;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetCartProductsByCartIdAsync - CartId: {CartId} - Exception: {E}", cartId, ex);
				return [];
			}
		}
	}
}
