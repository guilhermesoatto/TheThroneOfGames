using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets para suas entidades
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<GameEntity> Games { get; set; }
        public DbSet<PurchaseEntity> Purchases { get; set; }
        public DbSet<PromotionEntity> Promotions { get; set; }
        // ... outros DbSets

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Aplica todas as configurações de entidades definidas na pasta Configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
