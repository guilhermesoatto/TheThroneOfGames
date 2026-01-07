using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GameStore.Catalogo.Domain.Entities;
using GameStore.Catalogo.Domain.Interfaces;
using GameStore.Catalogo.Infrastructure.Persistence;

namespace GameStore.Catalogo.Infrastructure.Repository
{
    public class JogoRepository : IJogoRepository
    {
        private readonly CatalogoDbContext _context;

        public JogoRepository(CatalogoDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Jogo>> GetAllAsync()
        {
            return await _context.Jogos.ToListAsync();
        }

        public async Task<Jogo?> GetByIdAsync(Guid id)
        {
            return await _context.Jogos.FindAsync(id);
        }

        public async Task AddAsync(Jogo jogo)
        {
            await _context.Jogos.AddAsync(jogo);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Jogo jogo)
        {
            _context.Jogos.Update(jogo);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var jogo = await GetByIdAsync(id);
            if (jogo != null)
            {
                _context.Jogos.Remove(jogo);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Jogo>> GetByGeneroAsync(string genero)
        {
            return await _context.Jogos
                .Where(j => j.Genero.ToLower() == genero.ToLower())
                .ToListAsync();
        }

        public async Task<IEnumerable<Jogo>> GetDisponiveisAsync()
        {
            return await _context.Jogos
                .Where(j => j.Disponivel)
                .ToListAsync();
        }

        public async Task<IEnumerable<Jogo>> GetByNomeAsync(string nome)
        {
            return await _context.Jogos
                .Where(j => j.Nome.ToLower().Contains(nome.ToLower()))
                .ToListAsync();
        }

        public async Task<IEnumerable<Jogo>> GetByFaixaPrecoAsync(decimal precoMinimo, decimal precoMaximo)
        {
            return await _context.Jogos
                .Where(j => j.Preco >= precoMinimo && j.Preco <= precoMaximo)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Jogos.AnyAsync(j => j.Id == id);
        }
    }
}