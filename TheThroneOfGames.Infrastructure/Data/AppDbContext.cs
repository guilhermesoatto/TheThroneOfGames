using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets para suas entidades
        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        // ... outros DbSets

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Aplica todas as configurações de entidades definidas na pasta Configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
