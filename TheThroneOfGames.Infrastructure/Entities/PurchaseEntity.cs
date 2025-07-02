using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheThroneOfGames.Infrastructure.Entities
{
    public class PurchaseEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        // Relacionamento muitos-para-muitos com GameEntity
        public ICollection<GameEntity> Games { get; set; } = new List<GameEntity>();
        public DateTime PurchaseDate { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
