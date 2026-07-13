using System;

namespace GameStore.Usuarios.Domain.Entities
{
    /// <summary>
    /// Um jogo que o usuário já comprou. Populado a partir do GameCompradoEvent (publicado por
    /// GameStore.Vendas ao finalizar um pedido) — ver GameCompradoEventConsumer. Fonte da
    /// verdade para "o jogador possui este jogo?" (ver UsuarioController.PossuiJogo), usada
    /// pelo bounded context de Partidas antes de liberar a busca por partida.
    /// </summary>
    public class ItemInventario
    {
        protected ItemInventario() { } // EF Core

        public Guid Id { get; private set; }
        public Guid UsuarioId { get; private set; }
        public Guid JogoId { get; private set; }
        public string NomeJogo { get; private set; } = null!;
        public DateTime AdquiridoEm { get; private set; }

        public ItemInventario(Guid usuarioId, Guid jogoId, string nomeJogo)
        {
            if (usuarioId == Guid.Empty)
                throw new ArgumentException("UsuarioId é obrigatório", nameof(usuarioId));
            if (jogoId == Guid.Empty)
                throw new ArgumentException("JogoId é obrigatório", nameof(jogoId));

            Id = Guid.NewGuid();
            UsuarioId = usuarioId;
            JogoId = jogoId;
            NomeJogo = nomeJogo ?? string.Empty;
            AdquiridoEm = DateTime.UtcNow;
        }
    }
}
