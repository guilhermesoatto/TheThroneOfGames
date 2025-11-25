using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Infrastructure.Entities;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Domain.Events;

namespace GameStore.Vendas.Application
{
    public class PedidoService
    {
        private readonly IBaseRepository<Purchase> _purchaseRepository;
        private readonly IEventBus _eventBus;

        public PedidoService(IBaseRepository<Purchase> purchaseRepository, IEventBus eventBus)
        {
            _purchaseRepository = purchaseRepository;
            _eventBus = eventBus;
        }

        public async Task AddPurchaseAsync(Purchase purchase)
        {
            await _purchaseRepository.AddAsync(purchase);
            
            // Publish domain event
            var pedidoFinalizadoEvent = new PedidoFinalizadoEvent(
                PedidoId: purchase.Id,
                UserId: purchase.UserId,
                TotalPrice: 0m,  // Price not available in Purchase entity
                ItemCount: 1  // One item per purchase in this simplified model
            );
            await _eventBus.PublishAsync(pedidoFinalizadoEvent);
        }

        public async Task<IEnumerable<Purchase>> GetAllPurchasesAsync()
        {
            return await _purchaseRepository.GetAllAsync();
        }

        public async Task<Purchase?> GetByIdAsync(Guid id)
        {
            return await _purchaseRepository.GetByIdAsync(id);
        }
    }
}
