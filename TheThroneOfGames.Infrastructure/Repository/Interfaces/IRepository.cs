using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TheThroneOfGames.Infrastructure.Repository.Interfaces
{
    // Interface gen�rica para reposit�rios
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(Guid id);
    }
}
