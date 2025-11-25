using System;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Events;

namespace GameStore.Vendas.Application.EventHandlers
{
    /// <summary>
    /// Handler que reage quando um pedido é finalizado.
    /// Sincroniza informações de pedido no contexto de Vendas.
    /// </summary>
    public class PedidoFinalizadoEventHandler : IEventHandler<PedidoFinalizadoEvent>
    {
        public Task HandleAsync(PedidoFinalizadoEvent domainEvent)
        {
            // Log ou processamento quando um pedido é finalizado
            Console.WriteLine($"[Vendas] Notificação: Pedido {domainEvent.PedidoId} do usuário {domainEvent.UserId} foi finalizado. Total: R$ {domainEvent.TotalPrice} ({domainEvent.ItemCount} item(ns)).");
            
            // Aqui poderíamos:
            // - Registrar a transação em auditoria
            // - Enviar confirmação de entrega
            // - Atualizar estoque
            // - Gerar relatório de vendas
            // - Notificar sistema de fulfilment
            
            return Task.CompletedTask;
        }
    }
}
