using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Domain.Purchase
{
    public class PurchaseDomain :PurchaseEntity
    {
        public PurchaseDomain(Guid userId, Guid gameId, decimal totalPrice)
        {
            if (gameId == Guid.Empty) throw new ArgumentException("ID do jogo é obrigatório.");
            if (totalPrice <= 0) throw new ArgumentException("O valor total deve ser maior que zero.");

            Id = Guid.NewGuid();
            UserId = userId;
            GameId = gameId;
            PurchaseDate = DateTime.UtcNow;
            TotalPrice = totalPrice;
        }
    }
}
