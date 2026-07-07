using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using GameStore.Catalogo.Domain.Entities;

namespace GameStore.Catalogo.Infrastructure.Search;

public class ElasticsearchJogoIndexer : IJogoSearchIndexer
{
    public const string IndexName = "jogos";

    private readonly ElasticsearchClient _client;

    public ElasticsearchJogoIndexer(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task IndexAsync(Jogo jogo, CancellationToken ct = default)
    {
        var document = new JogoSearchDocument
        {
            Id = jogo.Id,
            Nome = jogo.Nome,
            Genero = jogo.Genero,
            Preco = jogo.Preco,
            Descricao = jogo.Descricao,
            Disponivel = jogo.Disponivel
        };

        var response = await _client.IndexAsync(document, idx => idx.Index(IndexName).Id(document.Id), ct);
        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Falha ao indexar jogo {document.Id} no Elasticsearch: {response.DebugInformation}");
        }
    }

    public async Task DeleteAsync(Guid jogoId, CancellationToken ct = default)
    {
        await _client.DeleteAsync<JogoSearchDocument>(jogoId, idx => idx.Index(IndexName), ct);
    }

    public async Task<IReadOnlyList<JogoSearchDocument>> SearchAsync(string termo, CancellationToken ct = default)
    {
        var response = await _client.SearchAsync<JogoSearchDocument>(s => s
            .Indices(IndexName)
            .Query(q => q.MultiMatch(m => m
                .Query(termo)
                .Fields(new[] { "nome", "genero", "descricao" })
            )), ct);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Falha ao buscar '{termo}' no Elasticsearch: {response.DebugInformation}");
        }

        return response.Documents.ToList();
    }
}
