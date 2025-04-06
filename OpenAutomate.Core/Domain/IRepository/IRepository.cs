using System.Linq.Expressions;

namespace OpenAutomate.Core.Domain.IRepository
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TEntity> GetByIdAsync(int id);
        Task<TEntity> GetByIdAsync(string id);
        Task<TEntity> GetByIdAsync(Guid id);

        Task<TEntity> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter = null,
            params Expression<Func<TEntity, object>>[] includes);

        Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            params Expression<Func<TEntity, object>>[] includes);

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> filter = null);

        Task AddAsync(TEntity entity);
        Task AddRangeAsync(IEnumerable<TEntity> entities);
        void Remove(TEntity entity);
        void RemoveRange(IEnumerable<TEntity> entities);
        void Update(TEntity entity);
        void UpdateRange(IEnumerable<TEntity> entities);
        Task UpdatePropertyAsync(Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, object>> propertyExpression, object newValue);

        Task<TEntity> UpdateOneAsync<TField>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TField>> field,
            TField value);
        Task<int> SaveChangesAsync();
    }
}