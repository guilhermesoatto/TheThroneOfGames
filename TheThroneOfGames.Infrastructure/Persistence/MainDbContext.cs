using Microsoft.EntityFrameworkCore;
using MimeKit;
using TheThroneOfGames.Domain.Entities;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.Infrastructure.Persistence;

public class MainDbContext : DbContext
{
    public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }


    public DbSet<GameEntity> Games { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração de entidades
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuario");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).IsRequired();
            entity.Property(u => u.ActiveToken);
            entity.Property(u => u.IsActive);
        });

        modelBuilder.Entity<GameEntity>(entity =>
        {
            entity.ToTable("GameEntity");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
            entity.Property(g => g.Genre).IsRequired().HasMaxLength(50);
            entity.Property(g => g.Price).IsRequired().HasPrecision(18, 2);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Promotion");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Discount).IsRequired().HasPrecision(18, 2);
            entity.Property(p => p.ValidUntil).IsRequired();
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.ToTable("Purchase");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.GameId).IsRequired();
            entity.Property(p => p.PurchaseDate).IsRequired();

            entity.HasOne<Usuario>().WithMany().HasForeignKey(p => p.UserId);
            entity.HasOne<GameEntity>().WithMany().HasForeignKey(p => p.GameId);
        });
    }
}