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
        public DbSet<ItemInventario> ItensInventario { get; set; }

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

            // Configure ItemInventario entity — jogos que o usuário já comprou (ver
            // GameCompradoEventConsumer). Um jogador nunca tem o mesmo jogo duas vezes.
            modelBuilder.Entity<ItemInventario>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.UsuarioId).IsRequired();
                entity.Property(i => i.JogoId).IsRequired();
                entity.Property(i => i.NomeJogo).IsRequired().HasMaxLength(200);
                entity.Property(i => i.AdquiridoEm).IsRequired();

                entity.HasIndex(i => new { i.UsuarioId, i.JogoId }).IsUnique();
            });
        }
    }
}