using System;
using System.Threading.Tasks;
using GameStore.Common.Events;

namespace GameStore.Usuarios.Application.EventHandlers
{
    /// <summary>
    /// Handler que reage quando um jogo é comprado.
    /// Sincroniza informações de compra no contexto de Usuários.
    /// </summary>
    public class GameCompradoEventHandler : IEventHandler<GameCompradoEvent>
    {
        public Task HandleAsync(GameCompradoEvent domainEvent)
        {
            // Log ou processamento quando um jogo é comprado no contexto de Catálogo/Vendas
            Console.WriteLine($"[Usuários] Notificação: Usuário {domainEvent.UserId} comprou o jogo '{domainEvent.NomeJogo}' por R$ {domainEvent.Preco}.");
            
            // Aqui poderíamos:
            // - Atualizar histórico de compras do usuário
            // - Incrementar badge de "Colecionador"
            // - Enviar email de confirmação
            // - Atualizar estatísticas de usuário
            
            return Task.CompletedTask;
        }
    }
}
