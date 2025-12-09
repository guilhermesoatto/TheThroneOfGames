using GameStore.Vendas.Application.Queries;
using GameStore.Vendas.Application.DTOs;
using GameStore.Vendas.Application.Mappers;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Domain.Interfaces;
using GameStore.CQRS.Abstractions;

namespace GameStore.Vendas.Application.Handlers
{
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
            var purchase = await _purchaseRepository.GetByIdAsync(query.PurchaseId);
            return purchase != null ? PurchaseMapper.ToPurchaseDTO(purchase) : null;
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
            var purchases = await _purchaseRepository.GetByUserIdAsync(query.UserId);
            return PurchaseMapper.ToPurchaseDTOList(purchases);
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
            var purchases = await _purchaseRepository.GetAllAsync();
            return PurchaseMapper.ToPurchaseDTOList(purchases);
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
            var purchases = await _purchaseRepository.GetByStatusAsync(query.Status);
            return PurchaseMapper.ToPurchaseDTOList(purchases);
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
            var purchases = await _purchaseRepository.GetByDateRangeAsync(query.StartDate, query.EndDate);
            return PurchaseMapper.ToPurchaseDTOList(purchases);
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
            var purchases = await _purchaseRepository.GetByGameIdAsync(query.GameId);
            return PurchaseMapper.ToPurchaseDTOList(purchases);
        }
    }

    /// <summary>
    /// Handler para GetSalesStatsQuery.
    /// </summary>
    public class GetSalesStatsQueryHandler : IQueryHandler<GetSalesStatsQuery, SalesStatsDTO>
    {
        private readonly IPurchaseRepository _purchaseRepository;

        public GetSalesStatsQueryHandler(IPurchaseRepository purchaseRepository)
        {
            _purchaseRepository = purchaseRepository;
        }

        public async Task<SalesStatsDTO> HandleAsync(GetSalesStatsQuery query)
        {
            var allPurchases = await _purchaseRepository.GetAllAsync();
            
            // Filtrar por período se especificado
            var filteredPurchases = allPurchases.AsEnumerable();
            
            if (query.StartDate.HasValue)
            {
                filteredPurchases = filteredPurchases.Where(p => p.PurchaseDate >= query.StartDate.Value);
            }
            
            if (query.EndDate.HasValue)
            {
                filteredPurchases = filteredPurchases.Where(p => p.PurchaseDate <= query.EndDate.Value);
            }

            var purchases = filteredPurchases.ToList();
            
            return new SalesStatsDTO
            {
                TotalPurchases = purchases.Count,
                TotalRevenue = purchases.Sum(p => p.TotalPrice),
                AveragePurchaseValue = purchases.Any() ? purchases.Average(p => p.TotalPrice) : 0,
                CompletedPurchases = purchases.Count(p => p.Status == "Completed"),
                PendingPurchases = purchases.Count(p => p.Status == "Pending"),
                CancelledPurchases = purchases.Count(p => p.Status == "Cancelled"),
                PurchasesByStatus = purchases.GroupBy(p => p.Status)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RevenueByGame = purchases.GroupBy(p => p.GameId.ToString())
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalPrice))
            };
        }
    }
}
