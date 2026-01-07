using Microsoft.EntityFrameworkCore;
using GameStore.Catalogo.Domain.Entities;

namespace GameStore.Catalogo.Infrastructure.Persistence
{
    public class CatalogoDbContext : DbContext
    {
        public CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : base(options)
        {
        }

        public DbSet<Jogo> Jogos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Jogo entity
            modelBuilder.Entity<Jogo>(entity =>
            {
                entity.HasKey(j => j.Id);
                entity.Property(j => j.Nome).IsRequired().HasMaxLength(200);
                entity.Property(j => j.Descricao).IsRequired().HasMaxLength(1000);
                entity.Property(j => j.Preco).HasPrecision(18, 2);
                entity.Property(j => j.Genero).IsRequired().HasMaxLength(50);
                entity.Property(j => j.Desenvolvedora).IsRequired().HasMaxLength(100);
                entity.Property(j => j.ImagemUrl).IsRequired().HasMaxLength(500);
                entity.Property(j => j.Disponivel).IsRequired();
                entity.Property(j => j.Estoque).IsRequired();

                // Indexes for performance
                entity.HasIndex(j => j.Genero);
                entity.HasIndex(j => j.Disponivel);
                entity.HasIndex(j => j.Preco);
                entity.HasIndex(j => j.Nome);
            });
        }
    }
}