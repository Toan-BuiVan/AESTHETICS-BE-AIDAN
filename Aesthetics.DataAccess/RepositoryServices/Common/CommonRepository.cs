using Aesthetics.Data.AestheticsDbContext;
using Aesthetics.Data.RepositoryInterfaces.Common;
using Aesthetics.DataAccess.RepositoryServices.Common;
using Aesthetics.Entities.BaseEntity;
using Aesthetics.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.RepositoryServices.Common
{
	public class CommonRepository<T> : ICommonRepository<T> where T : BaseEntity
	{
		protected readonly ILogger<CommonRepository<T>> _logger;
		protected readonly AestheticsDbContext.AestheticsDbContext _dbContext;
		protected readonly string nameEntity = typeof(T).Name;

		public CommonRepository(ILogger<CommonRepository<T>> logger, AestheticsDbContext.AestheticsDbContext dbContext)
		{
			_logger = logger;
			_dbContext = dbContext;
		}

		public async Task<bool> CreateEntity(T entity)
		{
			try
			{
				_dbContext.Set<T>().Add(entity);
				await _dbContext.SaveChangesAsync();
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create Entity {Na} - Exception: {E}", nameEntity, ex);
				return false;
			}
		}

		public async Task<bool> CreateRangeEntities(IEnumerable<T> entities)
		{
			try
			{
				_dbContext.Set<T>().AddRange(entities);
				await _dbContext.SaveChangesAsync();
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Create Range Entities {Na} - Exception: {E}", nameEntity, ex);
				return false;
			}
		}

		public async Task<bool> DeleteEntitiesStatus(T entity)
		{
			try
			{
				var trackedEntity = _dbContext.Set<T>().Find(entity.Id);

				if (trackedEntity == null)
					return false;

				trackedEntity.DeleteStatus = true;
				await _dbContext.SaveChangesAsync();
				_logger.LogInformation("Soft Delete {Na} - Id: {Id}", nameEntity, entity.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Soft Delete {Na} - Id: {Id}", nameEntity, entity.Id);
				return false;
			}
		}

		public async Task<bool> DeleteEntity(T entity)
		{
			try
			{
				_dbContext.Set<T>().Remove(entity);
				var result = await _dbContext.SaveChangesAsync();
				return result > 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "DeleteEntity {Na} - Exception: {E}", nameEntity, ex);
				return false;
			}

		}

		public async Task<bool> DeleteRangeEntities(IEnumerable<T> entities)
		{
			try
			{
				_dbContext.Set<T>().RemoveRange(entities);
				var result = await _dbContext.SaveChangesAsync();
				return result > 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "DeleteRangeEntity {Na} - Exception: {E}", nameEntity, ex);
				return false;
			}

		}

		public async Task<bool> DeleteRangeEntitiesStatus(T entity) 
		{
			try
			{
				var trackedEntity = await _dbContext.Set<T>().FindAsync(entity.Id);
				if (trackedEntity == null)
					return false;

				trackedEntity.DeleteStatus = true;

				var entry = _dbContext.Entry(trackedEntity);
				var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
				await SoftDeleteAsync(entry, visited);

				await _dbContext.SaveChangesAsync();
				_logger.LogInformation("Soft Delete {Entity} - Id: {Id}", typeof(T).Name, entity.Id);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Soft Delete {Entity} - Id: {Id}", typeof(T).Name, entity.Id);
				return false;
			}
		}

		private async Task SoftDeleteAsync(EntityEntry entry, HashSet<object> visited)
		{
			if (entry.Entity == null || !visited.Add(entry.Entity))
				return;

			var entityType = entry.Entity.GetType().Name;

			// Soft delete entity hiện tại (trừ Invoice và InvoiceDetail)
			if (entry.Entity is BaseEntity baseEntity &&
				entry.Entity is not InvoiceEntity &&
				entry.Entity is not InvoiceDetailEntity)
			{
				baseEntity.DeleteStatus = true;
			}

			// Nếu entity nằm trong NoCascadeTypes, dừng cascading
			if (CascadeDeleteConfiguration.NoCascadeTypes.Contains(entityType))
			{
				return;
			}

			// Lấy danh sách child entities được phép cascading
			var allowedChildren = CascadeDeleteConfiguration.GetAllowedChildren(entityType);

			// Nếu entity không có rule cascading, dừng tại đây
			if (allowedChildren == null || allowedChildren.Count == 0)
			{
				return;
			}

			foreach (var navigation in entry.Navigations)
			{
				var childEntityType = navigation.Metadata.TargetEntityType.Name;

				// Chỉ xử lý navigations được phép cascading
				if (!allowedChildren.Contains(childEntityType))
					continue;

				// Kiểm tra lại với hàm helper
				if (!CascadeDeleteConfiguration.IsAllowedCascade(entityType, childEntityType))
					continue;

				await navigation.LoadAsync();

				if (navigation.CurrentValue is IEnumerable<object> collection)
				{
					foreach (var child in collection)
					{
						var childEntry = _dbContext.Entry(child);
						await SoftDeleteAsync(childEntry, visited);
					}
				}
				else if (navigation.CurrentValue != null)
				{
					var childEntry = _dbContext.Entry(navigation.CurrentValue);
					await SoftDeleteAsync(childEntry, visited);
				}
			}
		}

		public async Task<ICollection<T>> FindByPredicate(Expression<Func<T, bool>> predicate)
		{
			try
			{
				var query = _dbContext.Set<T>().AsNoTracking().Where(predicate);
				var res = await query.ToListAsync();
				return res;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Entity {Na} - FindByPredicate - Exception: {E}", nameEntity, ex);
				return [];
			}
		}

		public async Task<T?> GetById(int id)
		{
			try
			{
				return await _dbContext.Set<T>()
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Id == id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Entity {Na} - GetById: {Id} - Exception: {E}", nameEntity, id, ex);
				return null;
			}
		}

		public async Task<bool> UpdateEntity(T entity)
		{
			try
			{
				_logger.LogInformation("Updating Entity {Na} with ID: {Id}", nameEntity, entity.Id);

				var trackedEntity = _dbContext.ChangeTracker
					  .Entries<T>()
					  .FirstOrDefault(e => e.Entity.Id == entity.Id);

				if (trackedEntity != null)
				{
					_dbContext.Entry(trackedEntity.Entity).State = EntityState.Detached;
				}
				_dbContext.Set<T>().Update(entity);
				var count = await _dbContext.SaveChangesAsync();
				_logger.LogInformation("Entity {Na} updated successfully. Rows affected: {Count}", nameEntity, count);
				return count > 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Entity {Na} {Id} updated fail: {Ex}", nameEntity, entity.Id, ex);
				_dbContext.Entry(entity).State = EntityState.Detached;
				return false;
			}
		}

		public async Task<bool> UpdateRangeEntities(IEnumerable<T> entities)
		{
			_logger.LogInformation("Start UpdateEntityRange {Na} - Count: {C}", nameEntity, entities.Count());
			if (!entities.Any()) return false;
			try
			{
				_dbContext.UpdateRange(entities);
				var count = await _dbContext.SaveChangesAsync();
				var check = count == entities.Count();
				_logger.LogInformation("Result UpdateEntityRange {Na} - {Co}", nameEntity, check);
				return check;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error UpdateEntityRange {Na} - Error: {Ex}", nameEntity, ex);
			}
			finally
			{
				_logger.LogInformation("End UpdateEntityRange {Na} - Count: {C}", nameEntity, entities.Count());
			}
			return false;
		}
	}
}
