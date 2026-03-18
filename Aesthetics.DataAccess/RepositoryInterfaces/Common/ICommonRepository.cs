using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aesthetics.Data.RepositoryInterfaces.Common
{
    public interface ICommonRepository<T> 
    {
        Task<bool> CreateEntity(T entity);
        Task<bool> CreateRangeEntities(IEnumerable<T> entities);
		Task<bool> UpdateEntity(T entity);
        Task<bool> UpdateRangeEntities(IEnumerable<T> entities);
		Task<bool> DeleteEntitiesStatus(T entity);
		Task<bool> DeleteRangeEntitiesStatus(T entity);
		Task<bool> DeleteEntity(T entity);
		Task<bool> DeleteRangeEntities(IEnumerable<T> entities);
        Task<T?> GetById(int id);
		Task<ICollection<T>> FindByPredicate(Expression<Func<T, bool>> predicate);
	}
}
