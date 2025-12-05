using GameStore.Usuarios.Application.DTOs;

namespace GameStore.Usuarios.Application.ReadModels
{
    /// <summary>
    /// Read Model para listagem de usuários (otimizado para grids).
    /// </summary>
    public class UsuarioListReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int PurchaseCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    /// <summary>
    /// Read Model para detalhes completos do usuário.
    /// </summary>
    public class UsuarioDetailReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }
        
        // Estatísticas
        public int PurchaseCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AveragePurchaseValue { get; set; }
        public int FavoriteGenreCount { get; set; }
        public List<string> FavoriteGenres { get; set; } = new();
        
        // Histórico recente
        public List<RecentPurchaseReadModel> RecentPurchases { get; set; } = new();
        public List<LoginHistoryReadModel> LoginHistory { get; set; } = new();
    }

    /// <summary>
    /// Read Model para compras recentes do usuário.
    /// </summary>
    public class RecentPurchaseReadModel
    {
        public Guid PurchaseId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public string GameGenre { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// Read Model para histórico de login.
    /// </summary>
    public class LoginHistoryReadModel
    {
        public DateTime LoginDate { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool WasSuccessful { get; set; }
        public string? FailureReason { get; set; }
    }

    /// <summary>
    /// Read Model para dashboard de usuários.
    /// </summary>
    public class UsuarioDashboardReadModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int UsersWithPurchases { get; set; }
        public decimal AverageUserSpent { get; set; }
        
        // Usuários por role
        public Dictionary<string, int> UsersByRole { get; set; } = new();
        
        // Crescimento mensal
        public List<MonthlyUserGrowthReadModel> MonthlyGrowth { get; set; } = new();
        
        // Top usuários
        public List<TopUserReadModel> TopUsersBySpending { get; set; } = new();
        public List<TopUserReadModel> TopUsersByPurchases { get; set; } = new();
    }

    /// <summary>
    /// Read Model para crescimento mensal de usuários.
    /// </summary>
    public class MonthlyUserGrowthReadModel
    {
        public string Month { get; set; } = string.Empty;
        public int NewUsers { get; set; }
        public int TotalUsers { get; set; }
        public double GrowthRate { get; set; }
    }

    /// <summary>
    /// Read Model para top usuários.
    /// </summary>
    public class TopUserReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal Value { get; set; } // Pode ser gasto total ou número de compras
        public int Rank { get; set; }
    }

    /// <summary>
    /// Read Model para perfil público do usuário.
    /// </summary>
    public class UsuarioPublicProfileReadModel
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime MemberSince { get; set; }
        public int PublicPurchaseCount { get; set; }
        public List<string> FavoriteGenres { get; set; } = new();
        public List<PublicGameReadModel> RecentPublicGames { get; set; } = new();
        public bool IsPublicProfile { get; set; }
    }

    /// <summary>
    /// Read Model para jogos no perfil público.
    /// </summary>
    public class PublicGameReadModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public int HoursPlayed { get; set; }
        public int AchievementCount { get; set; }
    }

    /// <summary>
    /// Mapper para converter entre DTOs e Read Models.
    /// </summary>
    public static class UsuarioReadModelMapper
    {
        public static UsuarioListReadModel ToListReadModel(UsuarioDTO dto, int purchaseCount = 0, decimal totalSpent = 0m)
        {
            return new UsuarioListReadModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Email = dto.Email,
                Role = dto.Role,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt,
                PurchaseCount = purchaseCount,
                TotalSpent = totalSpent
            };
        }

        public static UsuarioDetailReadModel ToDetailReadModel(
            UsuarioDTO dto, 
            List<RecentPurchaseReadModel> recentPurchases = null,
            List<LoginHistoryReadModel> loginHistory = null,
            int purchaseCount = 0,
            decimal totalSpent = 0m,
            List<string> favoriteGenres = null)
        {
            return new UsuarioDetailReadModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Email = dto.Email,
                Role = dto.Role,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                PurchaseCount = purchaseCount,
                TotalSpent = totalSpent,
                AveragePurchaseValue = purchaseCount > 0 ? totalSpent / purchaseCount : 0m,
                FavoriteGenres = favoriteGenres ?? new List<string>(),
                RecentPurchases = recentPurchases ?? new List<RecentPurchaseReadModel>(),
                LoginHistory = loginHistory ?? new List<LoginHistoryReadModel>()
            };
        }

        public static UsuarioPublicProfileReadModel ToPublicProfileReadModel(
            UsuarioDTO dto,
            List<PublicGameReadModel> recentGames = null,
            int publicPurchaseCount = 0,
            List<string> favoriteGenres = null)
        {
            return new UsuarioPublicProfileReadModel
            {
                Id = dto.Id,
                DisplayName = dto.Name,
                MemberSince = dto.CreatedAt,
                PublicPurchaseCount = publicPurchaseCount,
                FavoriteGenres = favoriteGenres ?? new List<string>(),
                RecentPublicGames = recentGames ?? new List<PublicGameReadModel>(),
                IsPublicProfile = true
            };
        }
    }
}
