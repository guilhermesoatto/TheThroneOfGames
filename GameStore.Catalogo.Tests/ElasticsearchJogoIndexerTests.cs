using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using FluentAssertions;
using GameStore.Catalogo.Domain.Entities;
using GameStore.Catalogo.Infrastructure.Search;
using Testcontainers.Elasticsearch;
using Xunit;

namespace GameStore.Catalogo.Tests;

/// <summary>
/// Prova, contra um Elasticsearch real (Testcontainers), que a indexação (fase3-T04) e a busca
/// avançada de jogos funcionam de ponta a ponta — não é um mock.
/// </summary>
[Trait("Category", "Integration")]
public class ElasticsearchJogoIndexerTests : IAsyncLifetime
{
    private ElasticsearchContainer _container = null!;
    private ElasticsearchJogoIndexer _indexer = null!;

    public async Task InitializeAsync()
    {
        // O módulo Testcontainers.Elasticsearch sobe sempre com HTTPS + certificado
        // autoassinado + credenciais geradas para o usuário "elastic" (por design, não
        // configurável via env var sem quebrar o healthcheck interno do container -
        // ver https://dotnet.testcontainers.org/modules/elasticsearch/). A solução
        // correta não é desabilitar TLS, e sim configurar o client para confiar no
        // certificado autoassinado do container.
        _container = new ElasticsearchBuilder("docker.elastic.co/elasticsearch/elasticsearch:8.15.0")
            .Build();

        await _container.StartAsync();

        var settings = new ElasticsearchClientSettings(new Uri(_container.GetConnectionString()))
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll);
        var client = new ElasticsearchClient(settings);
        _indexer = new ElasticsearchJogoIndexer(client);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private static Jogo CreateJogo(string nome, string genero, decimal preco)
        => new(
            nome: nome,
            descricao: $"Descrição de {nome}",
            preco: preco,
            genero: genero,
            desenvolvedora: "Test Studio",
            dataLancamento: DateTime.UtcNow,
            imagemUrl: "http://test.com/image.jpg",
            estoque: 10);

    [Fact]
    public async Task IndexAsync_ThenSearchAsync_FindsIndexedGameByName()
    {
        // Arrange
        var jogo = CreateJogo("Elden Ring", "RPG", 199.90m);

        // Act
        await _indexer.IndexAsync(jogo);
        await WaitForIndexRefreshAsync();
        var results = await _indexer.SearchAsync("Elden");

        // Assert
        results.Should().ContainSingle(d => d.Id == jogo.Id && d.Nome == "Elden Ring" && d.Genero == "RPG");
    }

    [Fact]
    public async Task SearchAsync_ByGenre_FindsAllMatchingGames()
    {
        // Arrange
        await _indexer.IndexAsync(CreateJogo("Dark Souls", "RPG", 99.90m));
        await _indexer.IndexAsync(CreateJogo("FIFA 26", "Esportes", 249.90m));
        await WaitForIndexRefreshAsync();

        // Act
        var results = await _indexer.SearchAsync("RPG");

        // Assert
        results.Should().Contain(d => d.Nome == "Dark Souls");
        results.Should().NotContain(d => d.Nome == "FIFA 26");
    }

    [Fact]
    public async Task IndexAsync_UpdatingSameId_OverwritesPreviousDocument()
    {
        // Arrange
        var jogo = CreateJogo("Cyberpunk 2077", "RPG", 199.90m);
        await _indexer.IndexAsync(jogo);
        await WaitForIndexRefreshAsync();

        // Act - reindexar o mesmo Id com preço atualizado
        jogo.AtualizarInformacoes(
            nome: jogo.Nome,
            descricao: jogo.Descricao,
            preco: 49.90m,
            genero: jogo.Genero,
            desenvolvedora: jogo.Desenvolvedora,
            imagemUrl: jogo.ImagemUrl);
        await _indexer.IndexAsync(jogo);
        await WaitForIndexRefreshAsync();
        var results = await _indexer.SearchAsync("Cyberpunk");

        // Assert - apenas um documento, com o preço atualizado
        results.Should().ContainSingle(d => d.Id == jogo.Id);
        results.Single(d => d.Id == jogo.Id).Preco.Should().Be(49.90m);
    }

    private static async Task WaitForIndexRefreshAsync()
    {
        // Elasticsearch é near-real-time; aguarda o intervalo de refresh padrão do índice.
        await Task.Delay(1200);
    }
}
