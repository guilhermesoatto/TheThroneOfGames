using Microsoft.EntityFrameworkCore;
using MimeKit;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.Infrastructure.Persistence;

public class MainDbContext : DbContext
{
    public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }

    public DbSet<Usuario> Users { get; set; }
    public DbSet<GameEntity> Games { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<Purchase> Purchases { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração de entidades
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).IsRequired();
        });

        modelBuilder.Entity<GameEntity>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
            entity.Property(g => g.Genre).IsRequired().HasMaxLength(50);
            entity.Property(g => g.Price).IsRequired();
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Discount).IsRequired();
            entity.Property(p => p.ValidUntil).IsRequired();
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.GameId).IsRequired();
            entity.Property(p => p.PurchaseDate).IsRequired();
        });
    }
}