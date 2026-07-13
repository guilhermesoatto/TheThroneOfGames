using System;
using System.Threading;
using System.Threading.Tasks;
using GameStore.Usuarios.Domain.Entities;

namespace GameStore.Usuarios.Domain.Interfaces
{
    public interface IInventarioRepository
    {
        /// <summary>Idempotente — não duplica se o mesmo usuário já tiver o mesmo jogo.</summary>
        Task AdicionarSeNaoExistirAsync(ItemInventario item, CancellationToken ct = default);

        Task<bool> PossuiJogoAsync(Guid usuarioId, Guid jogoId, CancellationToken ct = default);
    }
}
