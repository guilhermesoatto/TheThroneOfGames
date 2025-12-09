using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheThroneOfGames.Application.DTO
{
    public class PromotionDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Discount { get; set; }
        public DateTime ValidUntil { get; set; }
        public List<Guid> GameIds { get; set; }
    }
}
