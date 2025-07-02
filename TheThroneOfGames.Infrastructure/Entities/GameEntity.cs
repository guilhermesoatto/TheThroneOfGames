using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheThroneOfGames.Infrastructure.Entities
{
    public class GameEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Genre { get; set; }
        public decimal Price { get; set; }
        // Relacionamento muitos-para-muitos com PurchaseEntity
        public ICollection<PurchaseEntity> Purchases { get; set; } = new List<PurchaseEntity>();
    }
}
