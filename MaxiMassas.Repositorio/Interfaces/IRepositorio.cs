using System.Linq.Expressions;

namespace MaxiMassas.Repositorio.Interfaces;

public interface IRepositorio<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void Remove(T entity);
    Task<bool> SaveChangesAsync();
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
}