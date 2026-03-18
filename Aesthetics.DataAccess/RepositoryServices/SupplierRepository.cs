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
	public class SupplierRepository : CommonRepository<SupplierEntity>, ISupplierRepository
	{
		public SupplierRepository(ILogger<CommonRepository<SupplierEntity>> logger, AestheticsDbContext.AestheticsDbContext dbContext) : base(logger, dbContext)
		{

		}

		public async Task<bool> delete(int supplierId)
		{
			using var transaction = await _dbContext.Database.BeginTransactionAsync();

			try
			{
				var supplier = await _dbContext.Suppliers
					.FirstOrDefaultAsync(x => x.Id == supplierId);

				if (supplier == null)
					return false;

				supplier.DeleteStatus = true;

				var products = await _dbContext.Products
					.Where(x => x.SupplierId == supplierId)
					.ToListAsync();

				var productIds = products.Select(x => x.Id).ToList();

				foreach (var product in products)
				{
					product.DeleteStatus = true;
				}

				var comments = await _dbContext.Comments
					.Where(x => x.ProductId != null && productIds.Contains(x.ProductId.Value))
					.ToListAsync();

				foreach (var comment in comments)
				{
					comment.DeleteStatus = true;
				}

				var CartProductEntitys = await _dbContext.CartProducts
					.Where(x => x.ProductId != null && productIds.Contains(x.ProductId.Value))
					.ToListAsync();

				foreach (var cart in CartProductEntitys)
				{
					cart.DeleteStatus = true;
				}

				await _dbContext.SaveChangesAsync();
				await transaction.CommitAsync();
				_logger.LogInformation("Soft Delete Supplier cascade success: {Id}", supplierId);
				return true;
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				_logger.LogError(ex, "Soft Delete Supplier cascade failed: {Id}", supplierId);
				return false;
			}
		}

		public async Task<SupplierEntity?> GetByName(string name)
		{
			try
			{
				var supplier = await _dbContext.Suppliers
					.AsNoTracking()
					.FirstOrDefaultAsync(x => x.SupplierName.ToLower() == name.ToLower());
				return supplier;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetByName Exception: SupplierName '{SupplierName}'", name);
				return null;
			}
		}
	}
}
