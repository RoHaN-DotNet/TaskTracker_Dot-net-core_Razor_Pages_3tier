using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskTrackerDAL.Data;
using TaskTrackerDAL.Interfaces.Generic;
namespace TaskTrackerDAL.Repositories.Generic
{
    public class GenericRepository<T> : IGenericFeature<T> where T : class
    {
        protected readonly TaskTrackerDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(TaskTrackerDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IReadOnlyList<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        public async Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().SingleOrDefaultAsync(predicate);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            return predicate is null
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(predicate);
        }
    }
}
