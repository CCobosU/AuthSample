using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> SingleOrDefaultAsync(Func<T, bool> predicate);
    Task<IEnumerable<T>> ListAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}
