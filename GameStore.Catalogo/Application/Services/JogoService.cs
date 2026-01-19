using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameStore.Catalogo.Domain.Entities;
using GameStore.Catalogo.Domain.Interfaces;
using GameStore.Catalogo.Application.Interfaces;

namespace GameStore.Catalogo.Application.Services
{
    public class JogoService : IJogoService
    {
        private readonly IJogoRepository _jogoRepository;

        public JogoService(IJogoRepository jogoRepository)
        {
            _jogoRepository = jogoRepository;
        }

        public async Task<IEnumerable<Jogo>> GetAllJogosAsync()
        {
            return await _jogoRepository.GetAllAsync();
        }

        public async Task<Jogo?> GetJogoByIdAsync(Guid id)
        {
            return await _jogoRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Jogo>> GetJogosByGeneroAsync(string genero)
        {
            if (string.IsNullOrWhiteSpace(genero))
                throw new ArgumentException("Gênero é obrigatório", nameof(genero));

            return await _jogoRepository.GetByGeneroAsync(genero);
        }

        public async Task<IEnumerable<Jogo>> GetJogosDisponiveisAsync()
        {
            return await _jogoRepository.GetDisponiveisAsync();
        }

        public async Task<IEnumerable<Jogo>> BuscarJogosPorNomeAsync(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome para busca é obrigatório", nameof(nome));

            return await _jogoRepository.GetByNomeAsync(nome);
        }

        public async Task<IEnumerable<Jogo>> GetJogosPorFaixaPrecoAsync(decimal precoMinimo, decimal precoMaximo)
        {
            if (precoMinimo < 0)
                throw new ArgumentException("Preço mínimo deve ser maior ou igual a zero", nameof(precoMinimo));

            if (precoMaximo < precoMinimo)
                throw new ArgumentException("Preço máximo deve ser maior ou igual ao preço mínimo", nameof(precoMaximo));

            return await _jogoRepository.GetByFaixaPrecoAsync(precoMinimo, precoMaximo);
        }

        public async Task AdicionarJogoAsync(string nome, string descricao, decimal preco, string genero,
                                           string desenvolvedora, DateTime dataLancamento, string imagemUrl, int estoque)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome é obrigatório", nameof(nome));

            if (preco < 0)
                throw new ArgumentException("Preço deve ser maior ou igual a zero", nameof(preco));

            if (estoque < 0)
                throw new ArgumentException("Estoque deve ser maior ou igual a zero", nameof(estoque));

            var jogo = new Jogo(nome, descricao, preco, genero, desenvolvedora, dataLancamento, imagemUrl, estoque);
            await _jogoRepository.AddAsync(jogo);
        }

        public async Task AtualizarJogoAsync(Guid id, string nome, string descricao, decimal preco,
                                           string genero, string desenvolvedora, string imagemUrl)
        {
            var jogo = await _jogoRepository.GetByIdAsync(id);
            if (jogo == null)
                throw new ArgumentException($"Jogo com ID {id} não encontrado", nameof(id));

            jogo.AtualizarInformacoes(nome, descricao, preco, genero, desenvolvedora, imagemUrl);
            await _jogoRepository.UpdateAsync(jogo);
        }

        public async Task AtualizarEstoqueAsync(Guid id, int novoEstoque)
        {
            var jogo = await _jogoRepository.GetByIdAsync(id);
            if (jogo == null)
                throw new ArgumentException($"Jogo com ID {id} não encontrado", nameof(id));

            jogo.AtualizarEstoque(novoEstoque);
            await _jogoRepository.UpdateAsync(jogo);
        }

        public async Task RemoverJogoAsync(Guid id)
        {
            if (!await _jogoRepository.ExistsAsync(id))
                throw new ArgumentException($"Jogo com ID {id} não encontrado", nameof(id));

            await _jogoRepository.DeleteAsync(id);
        }

        public async Task IndisponibilizarJogoAsync(Guid id)
        {
            var jogo = await _jogoRepository.GetByIdAsync(id);
            if (jogo == null)
                throw new ArgumentException($"Jogo com ID {id} não encontrado", nameof(id));

            jogo.Indisponibilizar();
            await _jogoRepository.UpdateAsync(jogo);
        }

        public async Task DisponibilizarJogoAsync(Guid id)
        {
            var jogo = await _jogoRepository.GetByIdAsync(id);
            if (jogo == null)
                throw new ArgumentException($"Jogo com ID {id} não encontrado", nameof(id));

            jogo.Disponibilizar();
            await _jogoRepository.UpdateAsync(jogo);
        }
    }
}