using System;
using TheThroneOfGames.Infrastructure.Data;
using TheThroneOfGames.Infrastructure.Repository.Interfaces;

namespace TheThroneOfGames.Infrastructure.Repository
{
    // Factory para criar inst�ncias de reposit�rios gen�ricos
    public class RepositoryFactory
    {
        private readonly AppDbContext _context;

        public RepositoryFactory(AppDbContext context)
        {
            _context = context;
        }

        public IRepository<T> CreateRepository<T>() where T : class
        {
            return new Repository<T>(_context);
        }
    }
}
