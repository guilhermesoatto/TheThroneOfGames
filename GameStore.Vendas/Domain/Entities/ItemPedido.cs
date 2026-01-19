using GameStore.Vendas.Domain.ValueObjects;

namespace GameStore.Vendas.Domain.Entities
{
    public class ItemPedido
    {
        public Guid Id { get; private set; }
        public Guid JogoId { get; private set; }
        public string NomeJogo { get; private set; } = string.Empty;
        public Money Preco { get; private set; } = Money.Zero;
        public DateTime DataAdicao { get; private set; }

        private ItemPedido() { } // EF Core

        public ItemPedido(Guid jogoId, string nomeJogo, Money preco)
        {
            Id = Guid.NewGuid();
            JogoId = jogoId;
            NomeJogo = nomeJogo ?? throw new ArgumentNullException(nameof(nomeJogo));
            Preco = preco ?? throw new ArgumentNullException(nameof(preco));
            DataAdicao = DateTime.UtcNow;
        }
    }
}