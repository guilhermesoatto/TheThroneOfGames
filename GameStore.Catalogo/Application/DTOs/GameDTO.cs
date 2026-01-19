namespace GameStore.Catalogo.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object para Game/Jogo.
    /// Contém informações essenciais do jogo para exibição no catálogo.
    /// </summary>
    public class GameDTO
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string Genre { get; set; } = null!;

        // Optional description expected by some tests
        public string? Description { get; set; }

        public decimal Price { get; set; }

        public bool IsAvailable { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO alternativo para Jogo local do contexto Catalogo.
    /// Mantém compatibilidade com estrutura local se necessário.
    /// </summary>
    public class JogoDTO
    {
        public int Id { get; set; }

        public string Nome { get; set; } = null!;

        public decimal Preco { get; set; }
    }
}
