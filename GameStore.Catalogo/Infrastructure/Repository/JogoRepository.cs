using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public async Task<IEnumerable<Jogo>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.Jogos.ToListAsync(ct);
        }

        public async Task<Jogo?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Jogos.FindAsync(new object[] { id }, ct);
        }

        public async Task AddAsync(Jogo jogo, CancellationToken ct = default)
        {
            await _context.Jogos.AddAsync(jogo, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Jogo jogo, CancellationToken ct = default)
        {
            _context.Jogos.Update(jogo);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var jogo = await GetByIdAsync(id, ct);
            if (jogo != null)
            {
                _context.Jogos.Remove(jogo);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task<IEnumerable<Jogo>> GetByGeneroAsync(string genero, CancellationToken ct = default)
        {
            return await _context.Jogos
                .Where(j => j.Genero.ToLower() == genero.ToLower())
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Jogo>> GetDisponiveisAsync(CancellationToken ct = default)
        {
            return await _context.Jogos
                .Where(j => j.Disponivel)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Jogo>> GetByNomeAsync(string nome, CancellationToken ct = default)
        {
            return await _context.Jogos
                .Where(j => j.Nome.ToLower().Contains(nome.ToLower()))
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Jogo>> GetByFaixaPrecoAsync(decimal precoMinimo, decimal precoMaximo, CancellationToken ct = default)
        {
            return await _context.Jogos
                .Where(j => j.Preco >= precoMinimo && j.Preco <= precoMaximo)
                .ToListAsync(ct);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Jogos.AnyAsync(j => j.Id == id, ct);
        }
    }
}