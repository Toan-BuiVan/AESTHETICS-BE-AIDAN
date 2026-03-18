using Aesthetics.Data.AestheticsInterfaces;
using Aesthetics.Data.RepositoryInterfaces;
using Aesthetics.Entities.Entities;
using Aesthetics.Entities.Models.RequestModel;
using Aesthetics.Entities.Models.ResponseModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.AestheticsServices
{
	public class SupplierSevice : ISupplierSevice
	{
		private readonly ILogger<SupplierSevice> _logger;
		private readonly ISupplierRepository _supplierRepository;
		public SupplierSevice(ILogger<SupplierSevice> logger, ISupplierRepository supplierRepository)
		{
			_logger = logger;
			_supplierRepository = supplierRepository;
		}

		public async Task<bool> create(RequestSupplier supplier)
		{
			try
			{
				var existingSupplier = await _supplierRepository.GetByName(supplier.SupplierName);
				if (existingSupplier != null)
				{
					_logger.LogWarning("Create Supplier failed: SupplierName {SupplierName}", supplier.SupplierName);
					return false;
				}
				var entity = new SupplierEntity
				{
					SupplierName = supplier.SupplierName,
					Address = supplier.Address.Trim(),
					Phone = supplier.Phone.Trim(),
					DeleteStatus = false
				};
				var created = await _supplierRepository.CreateEntity(entity);
				if (!created)
				{
					_logger.LogError("Create Supplier failed at repository level: {SupplierName}", supplier.SupplierName);
					return false;
				}
				_logger.LogInformation("Create Supplier success: {SupplierName}", supplier.SupplierName);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create Supplier exception: SupplierName {SupplierName}", supplier.SupplierName);
				return false;
			}
		}
		public async Task<bool> delete(SupplierDelete supplier)
		{
			try
			{
				_logger.LogInformation("Start deleting Supplier");
				var isSupplier = await _supplierRepository.GetById(supplier.Id);
				if (isSupplier == null)
				{
					_logger.LogWarning("Delete Supplier failed: Not found with Id {Id}", supplier.Id);
					return false;
				}
				var deleted = await _supplierRepository.DeleteRangeEntitiesStatus(isSupplier);
				if (!deleted)
				{
					_logger.LogError("Delete Supplier failed at repository level: Id {Id}", supplier.Id);
					return false;
				}
				_logger.LogInformation("Delete Supplier success: Id {Id}", supplier.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Delete Supplier exception: Id {Id}", supplier.Id);
				return false;
			}
		}

		public async Task<BaseDataCollection<SupplierEntity>> getlist(SupplierGet searchSupplier)
		{
			try
			{
				Expression<Func<SupplierEntity, bool>> predicate = x => true;  

				if (!string.IsNullOrWhiteSpace(searchSupplier.SupplierName))
				{
					predicate = x => x.SupplierName.ToLower().Contains(searchSupplier.SupplierName.ToLower());
				}
				var allMatching = await _supplierRepository.FindByPredicate(predicate);
				var totalCount = allMatching.Count;

				var pagedData = allMatching
					.OrderBy(x => x.SupplierName)  
					.Skip((searchSupplier.PageNo - 1) * searchSupplier.PageSize)
					.Take(searchSupplier.PageSize)
					.ToList();

				return new BaseDataCollection<SupplierEntity>(pagedData, totalCount, searchSupplier.PageNo, searchSupplier.PageSize);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetList Supplier exception");
				return new BaseDataCollection<SupplierEntity>(null, 0, searchSupplier.PageNo, searchSupplier.PageSize);
			}
		}

		public async Task<bool> update(SupplierUpdate supplier)
		{
			try
			{
				var existingSupplier = await _supplierRepository.GetById(supplier.Id);
				if (existingSupplier == null)
				{
					_logger.LogWarning("Update Supplier failed: Not found with Id {Id}", supplier.Id);
					return false;
				}

				if (existingSupplier.SupplierName != supplier.SupplierName)
				{
					var duplicate = await _supplierRepository.GetByName(supplier.SupplierName);
					if (duplicate != null)
					{
						_logger.LogWarning("Update Supplier failed: Duplicate SupplierName {SupplierName}", supplier.SupplierName);
						return false;
					}
				}

				existingSupplier.SupplierName = supplier.SupplierName;
				existingSupplier.Address = supplier.Address.Trim();
				existingSupplier.Phone = supplier.Phone.Trim();

				var updated = await _supplierRepository.UpdateEntity(existingSupplier);
				if (!updated)
				{
					_logger.LogError("Update Supplier failed at repository level: Id {Id}", supplier.Id);
					return false;
				}

				_logger.LogInformation("Update Supplier success: Id {Id}", supplier.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Update Supplier exception: Id {Id}", supplier.Id);
				return false;
			}
		}
	}
}
