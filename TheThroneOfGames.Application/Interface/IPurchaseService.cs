using TheThroneOfGames.Application.DTO;

namespace TheThroneOfGames.Application.Interface
{
    public interface IPurchaseService
    {
        Task PurchaseGameAsync(Guid userId, Guid gameId);
        Task<IEnumerable<PurchaseDto>> GetUserLibraryAsync(Guid userId);
    }
}
