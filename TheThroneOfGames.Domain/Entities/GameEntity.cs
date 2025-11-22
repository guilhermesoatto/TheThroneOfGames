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
        public string Name { get; set; } = null!;
        public string Genre { get; set; } = null!;
        public decimal Price { get; set; }
    }
}
