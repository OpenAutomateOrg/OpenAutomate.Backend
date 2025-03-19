using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OpenAutomate.Domain.Interfaces.IRepository;
using OpenAutomate.Infrastructure.DbContext;

namespace OpenAutomate.Domain.IRepository
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly DbSet<TEntity> _dbSet;

        public Repository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<TEntity>();
        }

        public async Task<TEntity> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<TEntity> GetByIdAsync(string id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<TEntity> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;
            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return await query.FirstOrDefaultAsync(filter);
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;
            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            if (filter != null) query = query.Where(filter);

            if (orderBy != null) query = orderBy(query);

            return await query.ToListAsync();
        }

        public async Task AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public void Remove(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public void Update(TEntity entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
        }

        public void UpdateRange(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities) _dbContext.Entry(entity).State = EntityState.Modified;
        }

        public async Task UpdatePropertyAsync(Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, object>> propertyExpression, object newValue)
        {
            var entityToUpdate = await _dbSet.FirstOrDefaultAsync(predicate);
            if (entityToUpdate == null) return;

            var propertyInfo = GetPropertyInfo(propertyExpression);
            if (propertyInfo == null) return;

            propertyInfo.SetValue(entityToUpdate, newValue);

            _dbContext.Entry(entityToUpdate).Property(propertyInfo.Name).IsModified = true;

            await _dbContext.SaveChangesAsync();
        }

        public async Task<TEntity> UpdateOneAsync<TField>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TField>> field, TField value)
        {
            var entity = await _dbSet.FirstOrDefaultAsync(filter);

            if (entity != null)
            {
                var memberExpression = (MemberExpression)field.Body;
                var propertyName = memberExpression.Member.Name;
                var propertyInfo = typeof(TEntity).GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(entity, value);
                    await _dbContext.SaveChangesAsync();
                }
            }

            return entity;
        }

        private PropertyInfo GetPropertyInfo(Expression<Func<TEntity, object>> expression)
        {
            if (expression == null) return null;

            var member = expression.Body as MemberExpression;
            if (member == null) return null;

            return member.Member as PropertyInfo;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
