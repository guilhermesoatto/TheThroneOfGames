using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.Domain.Purchase
{
    public class PurchaseDomain :PurchaseEntity
    {
        public PurchaseDomain(Guid userId, List<Guid> gameIds, decimal totalAmount)
        {
            if (gameIds == null || !gameIds.Any()) throw new ArgumentException("A compra deve incluir pelo menos um jogo.");
            if (totalAmount <= 0) throw new ArgumentException("O valor total deve ser maior que zero.");

            Id = Guid.NewGuid();
            UserId = userId;
            GameIds = gameIds;
            PurchaseDate = DateTime.UtcNow;
            TotalAmount = totalAmount;
        }
    }
}
