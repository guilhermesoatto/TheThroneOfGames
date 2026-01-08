using GameStore.Vendas.Domain.ValueObjects;
using GameStore.Common.Events;

namespace GameStore.Vendas.Domain.Entities
{
    public class Pedido
    {
        public Guid Id { get; private set; }
        public Guid UsuarioId { get; private set; }
        public DateTime DataCriacao { get; private set; }
        public string Status { get; private set; } = string.Empty;
        public Money ValorTotal { get; private set; } = Money.Zero;
        public string? MetodoPagamento { get; private set; }
        public DateTime? DataFinalizacao { get; private set; }
        public DateTime? DataCancelamento { get; private set; }
        public string? MotivoCancelamento { get; private set; }

        private readonly List<ItemPedido> _itens = new();
        public IReadOnlyCollection<ItemPedido> Itens => _itens.AsReadOnly();

        private Pedido() { } // EF Core

        public Pedido(Guid usuarioId)
        {
            Id = Guid.NewGuid();
            UsuarioId = usuarioId;
            DataCriacao = DateTime.UtcNow;
            Status = "Pendente";
            ValorTotal = Money.Zero;
        }

        public void AdicionarItem(Guid jogoId, string nomeJogo, Money preco)
        {
            if (Status != "Pendente")
                throw new InvalidOperationException("Não é possível adicionar itens a um pedido que não está pendente");

            var itemExistente = _itens.FirstOrDefault(i => i.JogoId == jogoId);
            if (itemExistente != null)
                throw new InvalidOperationException("Este jogo já foi adicionado ao pedido");

            var item = new ItemPedido(jogoId, nomeJogo, preco);
            _itens.Add(item);
            RecalcularValorTotal();
        }

        public void RemoverItem(Guid jogoId)
        {
            if (Status != "Pendente")
                throw new InvalidOperationException("Não é possível remover itens de um pedido que não está pendente");

            var item = _itens.FirstOrDefault(i => i.JogoId == jogoId);
            if (item == null)
                throw new InvalidOperationException("Item não encontrado no pedido");

            _itens.Remove(item);
            RecalcularValorTotal();
        }

        public void Finalizar(string metodoPagamento)
        {
            if (Status != "Pendente")
                throw new InvalidOperationException("Apenas pedidos pendentes podem ser finalizados");

            if (!_itens.Any())
                throw new InvalidOperationException("Não é possível finalizar um pedido sem itens");

            Status = "Finalizado";
            MetodoPagamento = metodoPagamento;
            DataFinalizacao = DateTime.UtcNow;

            // Publicar evento de domínio
            var evento = new PedidoFinalizadoEvent(
                PedidoId: Id,
                UserId: UsuarioId,
                TotalPrice: ValorTotal.Amount,
                ItemCount: _itens.Count
            );
            // Evento será publicado pelo application service
        }

        public void Cancelar(string motivo)
        {
            if (Status == "Finalizado")
                throw new InvalidOperationException("Não é possível cancelar um pedido já finalizado");

            if (Status == "Cancelado")
                throw new InvalidOperationException("Pedido já está cancelado");

            Status = "Cancelado";
            MotivoCancelamento = motivo;
            DataCancelamento = DateTime.UtcNow;
        }

        private void RecalcularValorTotal()
        {
            ValorTotal = new Money(_itens.Sum(i => i.Preco.Amount));
        }
    }
}