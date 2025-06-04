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
        public List<Guid> GameIds { get; set; } // Jogos comprados
        public DateTime PurchaseDate { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
