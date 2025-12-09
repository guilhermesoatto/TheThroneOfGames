namespace GameStore.Vendas.Application.ReadModels
{
    /// <summary>
    /// Read Model para listagem de compras (otimizado para grids).
    /// </summary>
    public class PurchaseListReadModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string GameGenre { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }

    /// <summary>
    /// Read Model para detalhes completos da compra.
    /// </summary>
    public class PurchaseDetailReadModel
    {
        public Guid Id { get; set; }
        public UserPurchaseReadModel User { get; set; } = new();
        public GamePurchaseReadModel Game { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? CancellationReason { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? RefundedAt { get; set; }
        public decimal? RefundedAmount { get; set; }
        public string? RefundReason { get; set; }
        
        // Informações de pagamento
        public PaymentInfoReadModel PaymentInfo { get; set; } = new();
        
        // Histórico de status
        public List<StatusHistoryReadModel> StatusHistory { get; set; } = new();
        
        // Informações adicionais
        public string? PromoCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public bool IsGift { get; set; }
        public string? GiftRecipientEmail { get; set; }
        public string? GiftMessage { get; set; }
    }

    /// <summary>
    /// Read Model para informações do usuário na compra.
    /// </summary>
    public class UserPurchaseReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime MemberSince { get; set; }
        public int TotalPurchases { get; set; }
        public decimal TotalSpent { get; set; }
    }

    /// <summary>
    /// Read Model para informações do jogo na compra.
    /// </summary>
    public class GamePurchaseReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public string? Developer { get; set; }
        public string? Publisher { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string? CoverImageUrl { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Read Model para informações de pagamento.
    /// </summary>
    public class PaymentInfoReadModel
    {
        public string Method { get; set; } = string.Empty;
        public string? CardLastFour { get; set; }
        public string? CardBrand { get; set; }
        public string? TransactionId { get; set; }
        public string? GatewayResponse { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public bool IsSuccessful { get; set; }
        public string? FailureReason { get; set; }
        public int Installments { get; set; }
        public decimal InstallmentValue { get; set; }
    }

    /// <summary>
    /// Read Model para histórico de status da compra.
    /// </summary>
    public class StatusHistoryReadModel
    {
        public string Status { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? ChangedBy { get; set; }
        public string? Reason { get; set; }
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Read Model para dashboard de vendas.
    /// </summary>
    public class VendasDashboardReadModel
    {
        // Métricas principais
        public int TotalPurchases { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePurchaseValue { get; set; }
        public int CompletedPurchases { get; set; }
        public int PendingPurchases { get; set; }
        public int CancelledPurchases { get; set; }
        public int RefundedPurchases { get; set; }
        
        // Métricas do período
        public int PurchasesToday { get; set; }
        public decimal RevenueToday { get; set; }
        public int PurchasesThisWeek { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public int PurchasesThisMonth { get; set; }
        public decimal RevenueThisMonth { get; set; }
        
        // Crescimento
        public double RevenueGrowthRate { get; set; }
        public double PurchaseGrowthRate { get; set; }
        public double ConversionRate { get; set; }
        
        // Análises
        public Dictionary<string, int> PurchasesByStatus { get; set; } = new();
        public Dictionary<string, decimal> RevenueByPaymentMethod { get; set; } = new();
        public Dictionary<string, int> PurchasesByGameGenre { get; set; } = new();
        public Dictionary<string, decimal> RevenueByMonth { get; set; } = new();
        
        // Top produtos e usuários
        public List<TopGameReadModel> TopSellingGames { get; set; } = new();
        public List<TopUserReadModel> TopBuyingUsers { get; set; } = new();
        
        // Tendências
        public List<DailyRevenueReadModel> DailyRevenueTrend { get; set; } = new();
        public List<HourlySalesReadModel> HourlySalesPattern { get; set; } = new();
    }

    /// <summary>
    /// Read Model para top jogos vendidos.
    /// </summary>
    public class TopGameReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal AveragePrice { get; set; }
        public int Rank { get; set; }
    }

    /// <summary>
    /// Read Model para top usuários compradores.
    /// </summary>
    public class TopUserReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int PurchaseCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AveragePurchaseValue { get; set; }
        public int Rank { get; set; }
    }

    /// <summary>
    /// Read Model para receita diária.
    /// </summary>
    public class DailyRevenueReadModel
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int PurchaseCount { get; set; }
        public decimal AveragePurchaseValue { get; set; }
    }

    /// <summary>
    /// Read Model para padrão de vendas horário.
    /// </summary>
    public class HourlySalesReadModel
    {
        public int Hour { get; set; }
        public int PurchaseCount { get; set; }
        public decimal Revenue { get; set; }
        public double PercentageOfTotal { get; set; }
    }

    /// <summary>
    /// Read Model para relatório de vendas.
    /// </summary>
    public class VendasReportReadModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        
        // Sumário executivo
        public decimal TotalRevenue { get; set; }
        public int TotalPurchases { get; set; }
        public decimal AveragePurchaseValue { get; set; }
        public double ConversionRate { get; set; }
        
        // Análises detalhadas
        public List<GamePerformanceReadModel> GamePerformance { get; set; } = new();
        public List<UserSegmentReadModel> UserSegments { get; set; } = new();
        public List<PaymentMethodAnalysisReadModel> PaymentMethods { get; set; } = new();
        
        // Insights
        public List<string> KeyInsights { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Read Model para performance de jogos.
    /// </summary>
    public class GamePerformanceReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal MarketShare { get; set; }
        public double GrowthRate { get; set; }
        public decimal AveragePrice { get; set; }
        public int RefundCount { get; set; }
        public double RefundRate { get; set; }
    }

    /// <summary>
    /// Read Model para segmentos de usuários.
    /// </summary>
    public class UserSegmentReadModel
    {
        public string SegmentName { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageSpent { get; set; }
        public double PercentageOfTotal { get; set; }
        public List<string> Characteristics { get; set; } = new();
    }

    /// <summary>
    /// Read Model para análise de métodos de pagamento.
    /// </summary>
    public class PaymentMethodAnalysisReadModel
    {
        public string Method { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal TotalValue { get; set; }
        public double UsagePercentage { get; set; }
        public decimal AverageValue { get; set; }
        public double SuccessRate { get; set; }
        public int FailureCount { get; set; }
    }
}
