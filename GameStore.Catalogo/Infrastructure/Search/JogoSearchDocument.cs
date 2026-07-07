namespace GameStore.Catalogo.Infrastructure.Search;

/// <summary>
/// Documento indexado no Elasticsearch para busca avançada de jogos no Catálogo.
/// </summary>
public class JogoSearchDocument
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Genero { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public bool Disponivel { get; set; }
}
