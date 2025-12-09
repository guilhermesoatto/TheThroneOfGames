using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Domain.Promotion
{
    public class PromotionDomain : PromotionEntity
    {
        public PromotionDomain(string title, string description, decimal discount, DateTime validUntil, List<Guid> gameIds)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Título da promoção é obrigatório.");
            if (discount <= 0 || discount > 100) throw new ArgumentException("Desconto deve estar entre 0 e 100.");
            if (validUntil <= DateTime.UtcNow) throw new ArgumentException("Data de validade deve ser futura.");
            if (gameIds == null || !gameIds.Any()) throw new ArgumentException("A promoção deve incluir pelo menos um jogo.");

            Id = Guid.NewGuid();
            Title = title;
            Description = description;
            Discount = discount;
            ValidUntil = validUntil;
            GameIds = gameIds;
        }
    }
}
