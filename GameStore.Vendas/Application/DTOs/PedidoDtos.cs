using GameStore.Vendas.Domain.ValueObjects;

namespace GameStore.Vendas.Application.DTOs
{
    public class PedidoDto
    {
        public Guid Id { get; set; }
        public Guid UsuarioId { get; set; }
        public DateTime DataCriacao { get; set; }
        public string Status { get; set; } = string.Empty;
        public Money ValorTotal { get; set; } = Money.Zero;
        public string? MetodoPagamento { get; set; }
        public DateTime? DataFinalizacao { get; set; }
        public DateTime? DataCancelamento { get; set; }
        public string? MotivoCancelamento { get; set; }
        public List<ItemPedidoDto> Itens { get; set; } = new();
    }

    public class ItemPedidoDto
    {
        public Guid Id { get; set; }
        public Guid JogoId { get; set; }
        public string NomeJogo { get; set; } = string.Empty;
        public Money Preco { get; set; } = Money.Zero;
        public DateTime DataAdicao { get; set; }
    }
}