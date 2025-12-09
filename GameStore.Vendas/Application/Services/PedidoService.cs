using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Domain.Events;

namespace GameStore.Vendas.Application
{
    public class PedidoService
    {
        private readonly IBaseRepository<PurchaseEntity> _purchaseRepository;
        private readonly IEventBus _eventBus;

        public PedidoService(IBaseRepository<PurchaseEntity> purchaseRepository, IEventBus eventBus)
        {
            _purchaseRepository = purchaseRepository;
            _eventBus = eventBus;
        }

        public async Task AddPurchaseAsync(PurchaseEntity purchase)
        {
            await _purchaseRepository.AddAsync(purchase);
            
            // Publish domain event
            var pedidoFinalizadoEvent = new PedidoFinalizadoEvent(
                PedidoId: purchase.Id,
                UserId: purchase.UserId,
                TotalPrice: 0m,
                ItemCount: 1
            );
            await _eventBus.PublishAsync(pedidoFinalizadoEvent);
        }

        public async Task<IEnumerable<PurchaseEntity>> GetAllPurchasesAsync()
        {
            return await _purchaseRepository.GetAllAsync();
        }

        public async Task<PurchaseEntity?> GetByIdAsync(Guid id)
        {
            return await _purchaseRepository.GetByIdAsync(id);
        }
    }
}
