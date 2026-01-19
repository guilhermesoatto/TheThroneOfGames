using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameStore.Catalogo.Domain.Entities;

namespace GameStore.Catalogo.Application.Interfaces
{
    public interface IJogoService
    {
        Task<IEnumerable<Jogo>> GetAllJogosAsync();
        Task<Jogo?> GetJogoByIdAsync(Guid id);
        Task<IEnumerable<Jogo>> GetJogosByGeneroAsync(string genero);
        Task<IEnumerable<Jogo>> GetJogosDisponiveisAsync();
        Task<IEnumerable<Jogo>> BuscarJogosPorNomeAsync(string nome);
        Task<IEnumerable<Jogo>> GetJogosPorFaixaPrecoAsync(decimal precoMinimo, decimal precoMaximo);
        Task AdicionarJogoAsync(string nome, string descricao, decimal preco, string genero,
                               string desenvolvedora, DateTime dataLancamento, string imagemUrl, int estoque);
        Task AtualizarJogoAsync(Guid id, string nome, string descricao, decimal preco,
                               string genero, string desenvolvedora, string imagemUrl);
        Task AtualizarEstoqueAsync(Guid id, int novoEstoque);
        Task RemoverJogoAsync(Guid id);
        Task IndisponibilizarJogoAsync(Guid id);
        Task DisponibilizarJogoAsync(Guid id);
    }
}