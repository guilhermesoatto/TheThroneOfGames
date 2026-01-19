using System;

namespace GameStore.Catalogo.Domain.Entities
{
    public class Jogo
    {
        // Parameterless constructor required by EF Core for materialization
        protected Jogo() { }

        public Guid Id { get; private set; }
        public string Nome { get; private set; } = null!;
        public string Descricao { get; private set; } = null!;
        public decimal Preco { get; private set; }
        public string Genero { get; private set; } = null!;
        public string Desenvolvedora { get; private set; } = null!;
        public DateTime DataLancamento { get; private set; }
        public bool Disponivel { get; private set; }
        public string ImagemUrl { get; private set; } = null!;
        public int Estoque { get; private set; }

        public Jogo(string nome, string descricao, decimal preco, string genero,
                   string desenvolvedora, DateTime dataLancamento, string imagemUrl, int estoque)
        {
            Id = Guid.NewGuid();
            Nome = nome;
            Descricao = descricao;
            Preco = preco;
            Genero = genero;
            Desenvolvedora = desenvolvedora;
            DataLancamento = dataLancamento;
            ImagemUrl = imagemUrl;
            Estoque = estoque;
            Disponivel = estoque > 0;
        }

        public Jogo(Guid id, string nome, string descricao, decimal preco, string genero,
                   string desenvolvedora, DateTime dataLancamento, string imagemUrl, int estoque, bool disponivel)
        {
            Id = id;
            Nome = nome;
            Descricao = descricao;
            Preco = preco;
            Genero = genero;
            Desenvolvedora = desenvolvedora;
            DataLancamento = dataLancamento;
            ImagemUrl = imagemUrl;
            Estoque = estoque;
            Disponivel = disponivel;
        }

        public void AtualizarInformacoes(string nome, string descricao, decimal preco,
                                       string genero, string desenvolvedora, string imagemUrl)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome é obrigatório", nameof(nome));

            if (preco < 0)
                throw new ArgumentException("Preço deve ser maior ou igual a zero", nameof(preco));

            Nome = nome;
            Descricao = descricao ?? string.Empty;
            Preco = preco;
            Genero = genero ?? string.Empty;
            Desenvolvedora = desenvolvedora ?? string.Empty;
            ImagemUrl = imagemUrl ?? string.Empty;
        }

        public void AtualizarEstoque(int novoEstoque)
        {
            if (novoEstoque < 0)
                throw new ArgumentException("Estoque não pode ser negativo", nameof(novoEstoque));

            Estoque = novoEstoque;
            Disponivel = novoEstoque > 0;
        }

        public void Indisponibilizar()
        {
            Disponivel = false;
        }

        public void Disponibilizar()
        {
            if (Estoque > 0)
                Disponivel = true;
        }
    }
}