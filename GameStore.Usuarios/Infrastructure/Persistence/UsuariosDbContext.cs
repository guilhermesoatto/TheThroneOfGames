using Microsoft.EntityFrameworkCore;
using GameStore.Usuarios.Domain.Entities;

namespace GameStore.Usuarios.Infrastructure.Persistence
{
    public class UsuariosDbContext : DbContext
    {
        public UsuariosDbContext(DbContextOptions<UsuariosDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Usuario entity
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).IsRequired().HasMaxLength(50);
                entity.Property(u => u.IsActive).IsRequired();
                entity.Property(u => u.ActiveToken).IsRequired(false).HasMaxLength(255);

                // Unique constraint on Email
                entity.HasIndex(u => u.Email).IsUnique();

                // Index on ActiveToken for faster lookups (nullable)
                entity.HasIndex(u => u.ActiveToken);
            });
        }
    }
}