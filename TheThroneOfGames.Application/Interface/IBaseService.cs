using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheThroneOfGames.Application.Interface
{
    public interface IBaseService<TEntity>
        where TEntity : class
    {
        Task AddAsync(TEntity entity);
        Task<TEntity?> GetByIdAsync(Guid id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(Guid id);
    }
}
