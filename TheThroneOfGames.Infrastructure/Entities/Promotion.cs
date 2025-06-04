using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheThroneOfGames.Infrastructure.Entities
{
    public class Promotion
    {
        public Guid Id { get; set; }
        public decimal Discount { get; set; }
        public DateTime ValidUntil { get; set; }
        
    }
}
