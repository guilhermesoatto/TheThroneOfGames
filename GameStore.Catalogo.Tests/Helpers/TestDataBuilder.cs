using System;
using GameStore.Catalogo.Domain.Entities;

namespace GameStore.Catalogo.Tests.Helpers
{
    public static class TestDataBuilder
    {
        /// <summary>
        /// Cria um Jogo para testes com valores padrão
        /// </summary>
        public static Jogo CreateDefaultJogo(
            string? nome = null,
            string? descricao = null,
            decimal? preco = null,
            string? genero = null,
            string? desenvolvedora = null,
            DateTime? dataLancamento = null,
            string? imagemUrl = null,
            int? estoque = null)
        {
            return new Jogo(
                nome: nome ?? "Test Game",
                descricao: descricao ?? "Test Description",
                preco: preco ?? 59.99m,
                genero: genero ?? "Action",
                desenvolvedora: desenvolvedora ?? "Test Developer",
                dataLancamento: dataLancamento ?? DateTime.UtcNow,
                imagemUrl: imagemUrl ?? "http://test.com/image.jpg",
                estoque: estoque ?? 100
            );
        }

        /// <summary>
        /// Cria um Jogo com ID específico para testes
        /// </summary>
        public static Jogo CreateJogoWithId(
            Guid id,
            string? nome = null,
            string? descricao = null,
            decimal? preco = null,
            string? genero = null,
            string? desenvolvedora = null,
            DateTime? dataLancamento = null,
            string? imagemUrl = null,
            int? estoque = null,
            bool? disponivel = null)
        {
            return new Jogo(
                id: id,
                nome: nome ?? "Test Game",
                descricao: descricao ?? "Test Description",
                preco: preco ?? 59.99m,
                genero: genero ?? "Action",
                desenvolvedora: desenvolvedora ?? "Test Developer",
                dataLancamento: dataLancamento ?? DateTime.UtcNow,
                imagemUrl: imagemUrl ?? "http://test.com/image.jpg",
                estoque: estoque ?? 100,
                disponivel: disponivel ?? true
            );
        }

        /// <summary>
        /// Cria um Jogo indisponível para testes
        /// </summary>
        public static Jogo CreateUnavailableJogo()
        {
            var jogo = CreateDefaultJogo(estoque: 0);
            return CreateJogoWithId(
                Guid.NewGuid(),
                jogo.Nome,
                jogo.Descricao,
                jogo.Preco,
                jogo.Genero,
                jogo.Desenvolvedora,
                jogo.DataLancamento,
                jogo.ImagemUrl,
                0,
                false
            );
        }
    }
}
