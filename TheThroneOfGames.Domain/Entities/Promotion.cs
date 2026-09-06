namespace TheThroneOfGames.Domain.Entities
{
    namespace Legacy
    {
        public class PromotionEntity_Legacy
        {
            public Guid Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public decimal Discount { get; set; }
            public DateTime ValidUntil { get; set; }
            public List<Guid> GameIds { get; set; } // Jogos associados à promoção
        }
    }
}
