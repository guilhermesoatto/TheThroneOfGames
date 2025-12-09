using GameStore.Vendas.Application.DTOs;
using GameStore.Vendas.Application.Mappers;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;
using GameStore.CQRS.Abstractions;

namespace GameStore.Vendas.Application.Queries
{
    /// <summary>
    /// Query para obter compra por ID.
    /// </summary>
    public record GetPurchaseByIdQuery(Guid PurchaseId) : IQuery<PurchaseDTO?>;

    /// <summary>
    /// Query para obter compras por usuário.
    /// </summary>
    public record GetPurchasesByUserQuery(Guid UserId) : IQuery<IEnumerable<PurchaseDTO>>;

    /// <summary>
    /// Query para obter todas as compras.
    /// </summary>
    public record GetAllPurchasesQuery() : IQuery<IEnumerable<PurchaseDTO>>;

    /// <summary>
    /// Query para obter compras por status.
    /// </summary>
    public record GetPurchasesByStatusQuery(string Status) : IQuery<IEnumerable<PurchaseDTO>>;

    /// <summary>
    /// Query para obter compras por período.
    /// </summary>
    public record GetPurchasesByDateRangeQuery(DateTime StartDate, DateTime EndDate) : IQuery<IEnumerable<PurchaseDTO>>;

    /// <summary>
    /// Query para obter compras por jogo.
    /// </summary>
    public record GetPurchasesByGameQuery(Guid GameId) : IQuery<IEnumerable<PurchaseDTO>>;

    /// <summary>
    /// Query para obter estatísticas de vendas.
    /// </summary>
    public record GetSalesStatsQuery(DateTime? StartDate = null, DateTime? EndDate = null) : IQuery<SalesStatsDTO>;

    /// <summary>
    /// DTO para estatísticas de vendas.
    /// </summary>
    public class SalesStatsDTO
    {
        public int TotalPurchases { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePurchaseValue { get; set; }
        public int CompletedPurchases { get; set; }
        public int PendingPurchases { get; set; }
        public int CancelledPurchases { get; set; }
        public Dictionary<string, int> PurchasesByStatus { get; set; } = new();
        public Dictionary<string, decimal> RevenueByGame { get; set; } = new();
    }

    /// <summary>
    /// Handler para GetPurchaseByIdQuery.
    /// </summary>
    public class GetPurchaseByIdQueryHandler : IQueryHandler<GetPurchaseByIdQuery, PurchaseDTO?>
    {
        private readonly IPurchaseRepository _purchaseRepository;

        public GetPurchaseByIdQueryHandler(IPurchaseRepository purchaseRepository)
        {
            _purchaseRepository = purchaseRepository;
        }

        public async Task<PurchaseDTO?> HandleAsync(GetPurchaseByIdQuery query)
        {
            try
            {
                var purchase = await _purchaseRepository.GetByIdAsync(query.PurchaseId);
                return purchase != null ? PurchaseMapper.ToPurchaseDTO(purchase) : null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Handler para GetPurchasesByUserQuery.
    /// </summary>
    public class GetPurchasesByUserQueryHandler : IQueryHandler<GetPurchasesByUserQuery, IEnumerable<PurchaseDTO>>
    {
        private readonly IPurchaseRepository _purchaseRepository;

        public GetPurchasesByUserQueryHandler(IPurchaseRepository purchaseRepository)
        {
            _purchaseRepository = purchaseRepository;
        }

        public async Task<IEnumerable<PurchaseDTO>> HandleAsync(GetPurchasesByUserQuery query)
        {
            try
            {
                var purchases = await _purchaseRepository.GetByUserIdAsync(query.UserId);
                return PurchaseMapper.ToPurchaseDTOList(purchases);
            }
            catch
            {
                return Enumerable.Empty<PurchaseDTO>();
            }
        }
    }

    /// <summary>
    /// Handler para GetAllPurchasesQuery.
    /// </summary>
    public class GetAllPurchasesQueryHandler : IQueryHandler<GetAllPurchasesQuery, IEnumerable<PurchaseDTO>>
    {
        private readonly IPurchaseRepository _purchaseRepository;

        public GetAllPurchasesQueryHandler(IPurchaseRepository purchaseRepository)
        {
            _purchaseRepository = purchaseRepository;
        }

        public async Task<IEnumerable<PurchaseDTO>> HandleAsync(GetAllPurchasesQuery query)
        {
            try
            {
                var purchases = await _purchaseRepository.GetAllAsync();
                return PurchaseMapper.ToPurchaseDTOList(purchases);
            }
            catch
            {
                return Enumerable.Empty<PurchaseDTO>();
            }
        }
    }

    /// <summary>
    /// Handler para GetPurchasesByStatusQuery.
    /// </summary>
    public class GetPurchasesByStatusQueryHandler : IQueryHandler<GetPurchasesByStatusQuery, IEnumerable<PurchaseDTO>>
    {
        private readonly IPurchaseRepository _purchaseRepository;

        public GetPurchasesByStatusQueryHandler(IPurchaseRepository purchaseRepository)
        {
            _purchaseRepository = purchaseRepository;
        }

        public async Task<IEnumerable<PurchaseDTO>> HandleAsync(GetPurchasesByStatusQuery query)
        {
            try
            {
                var purchases = await _purchaseRepository.GetByStatusAsync(query.Status);
                return PurchaseMapper.ToPurchaseDTOList(purchases);
            }
            catch
            {
                return Enumerable.Empty<PurchaseDTO>();
            }
        }
    }

    /// <summary>
    /// Handler para GetPurchasesByDateRangeQuery.
    /// </summary>
    public class GetPurchasesByDateRangeQueryHandler : IQueryHandler<GetPurchasesByDateRangeQuery, IEnumerable<PurchaseDTO>>
    {
        private readonly IPurchaseRepository _purchaseRepository;

        public GetPurchasesByDateRangeQueryHandler(IPurchaseRepository purchaseRepository)
        {
            _purchaseRepository = purchaseRepository;
        }

        public async Task<IEnumerable<PurchaseDTO>> HandleAsync(GetPurchasesByDateRangeQuery query)
        {
            try
            {
                var purchases = await _purchaseRepository.GetByDateRangeAsync(query.StartDate, query.EndDate);
                return PurchaseMapper.ToPurchaseDTOList(purchases);
            }
            catch
            {
                return Enumerable.Empty<PurchaseDTO>();
            }
        }
    }

    /// <summary>
    /// Handler para GetPurchasesByGameQuery.
    /// </summary>
    public class GetPurchasesByGameQueryHandler : IQueryHandler<GetPurchasesByGameQuery, IEnumerable<PurchaseDTO>>
    {
        private readonly IPurchaseRepository _purchaseRepository;

        public GetPurchasesByGameQueryHandler(IPurchaseRepository purchaseRepository)
        {
            _purchaseRepository = purchaseRepository;
        }

        public async Task<IEnumerable<PurchaseDTO>> HandleAsync(GetPurchasesByGameQuery query)
        {
            try
            {
                var purchases = await _purchaseRepository.GetByGameIdAsync(query.GameId);
                return PurchaseMapper.ToPurchaseDTOList(purchases);
            }
            catch
            {
                return Enumerable.Empty<PurchaseDTO>();
            }
        }
    }

    /// <summary>
    /// Handler para GetSalesStatsQuery.
    /// </summary>
    public class GetSalesStatsQueryHandler : IQueryHandler<GetSalesStatsQuery, SalesStatsDTO>
    {
        private readonly IPurchaseRepository _purchaseRepository;
        private readonly IGameRepository _gameRepository;

        public GetSalesStatsQueryHandler(IPurchaseRepository purchaseRepository, IGameRepository gameRepository)
        {
            _purchaseRepository = purchaseRepository;
            _gameRepository = gameRepository;
        }

        public async Task<SalesStatsDTO> HandleAsync(GetSalesStatsQuery query)
        {
            var stats = new SalesStatsDTO();

            try
            {
                // Obter todas as compras no período
                var purchases = query.StartDate.HasValue && query.EndDate.HasValue
                    ? await _purchaseRepository.GetByDateRangeAsync(query.StartDate.Value, query.EndDate.Value)
                    : await _purchaseRepository.GetAllAsync();

                // Estatísticas básicas
                stats.TotalPurchases = purchases.Count();
                stats.TotalRevenue = purchases.Where(p => p.Status == "Completed").Sum(p => p.TotalPrice);
                stats.AveragePurchaseValue = stats.TotalPurchases > 0 ? stats.TotalRevenue / stats.TotalPurchases : 0;

                // Contagem por status
                stats.CompletedPurchases = purchases.Count(p => p.Status == "Completed");
                stats.PendingPurchases = purchases.Count(p => p.Status == "Pending");
                stats.CancelledPurchases = purchases.Count(p => p.Status == "Cancelled");

                stats.PurchasesByStatus = purchases
                    .GroupBy(p => p.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Revenue por jogo
                var gameNames = new Dictionary<Guid, string>();
                foreach (var gameId in purchases.Select(p => p.GameId).Distinct())
                {
                    var game = await _gameRepository.GetByIdAsync(gameId);
                    if (game != null)
                    {
                        gameNames[gameId] = game.Name;
                    }
                }

                stats.RevenueByGame = purchases
                    .Where(p => p.Status == "Completed")
                    .GroupBy(p => p.GameId)
                    .ToDictionary(
                        g => gameNames.GetValueOrDefault(g.Key, g.Key.ToString()),
                        g => g.Sum(p => p.TotalPrice)
                    );
            }
            catch
            {
                // Retorna estatísticas vazias em caso de erro
            }

            return stats;
        }
    }
}
