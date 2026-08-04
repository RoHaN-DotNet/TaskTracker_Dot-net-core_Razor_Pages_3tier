
using System.Linq.Expressions;
using System.Text;

namespace TaskTrackerDAL.Interfaces.Generic
{
    public interface IGenericFeature<T> where T : class
    {
        Task<T?> GetByIdAsync(int? id);

        Task<IReadOnlyList<T>> GetAllAsync();

        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);

        Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate);

        Task AddAsync(T entity);

        void Update(T entity);

        void Remove(T entity);

        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    }
}

