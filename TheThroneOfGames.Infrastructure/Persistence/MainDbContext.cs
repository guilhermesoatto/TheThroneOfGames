using Microsoft.EntityFrameworkCore;
using MimeKit;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.Infrastructure.Persistence;

public class MainDbContext : DbContext
{
    public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }


    public DbSet<GameEntity> Games { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameEntity>().ToTable("Games");
        modelBuilder.Entity<User>().ToTable("Users");

        modelBuilder.Entity<GameEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Genre).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Price).IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Password).IsRequired();
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
        });
    }
}