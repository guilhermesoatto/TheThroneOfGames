using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheThroneOfGames.Application.DTO
{
    public class PurchaseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public List<GameDto> Games { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
