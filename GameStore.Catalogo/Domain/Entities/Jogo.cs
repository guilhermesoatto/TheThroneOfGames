namespace GameStore.Catalogo.Domain.Entities
{
    public class Jogo
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public decimal Preco { get; set; }
    }
}