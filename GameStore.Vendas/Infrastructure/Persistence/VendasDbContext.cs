using GameStore.Vendas.Domain.Entities;
using GameStore.Vendas.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Vendas.Infrastructure.Persistence
{
    public class VendasDbContext : DbContext
    {
        public VendasDbContext(DbContextOptions<VendasDbContext> options) : base(options) { }

        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<ItemPedido> ItensPedido { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da entidade Pedido
            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).ValueGeneratedNever();
                entity.Property(p => p.UsuarioId).IsRequired();
                entity.Property(p => p.DataCriacao).IsRequired();
                entity.Property(p => p.Status).IsRequired().HasMaxLength(20);
                entity.Property(p => p.MetodoPagamento).HasMaxLength(50);
                entity.Property(p => p.MotivoCancelamento).HasMaxLength(500);

                // Relacionamento com ItensPedido
                entity.HasMany(p => p.Itens)
                    .WithOne()
                    .HasForeignKey("PedidoId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuração da entidade ItemPedido
            modelBuilder.Entity<ItemPedido>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Id).ValueGeneratedNever();
                entity.Property(i => i.JogoId).IsRequired();
                entity.Property(i => i.NomeJogo).IsRequired().HasMaxLength(200);
                entity.Property(i => i.DataAdicao).IsRequired();
            });

            // Configuração do Value Object Money
            modelBuilder.Entity<Pedido>()
                .OwnsOne(p => p.ValorTotal, money =>
                {
                    money.Property(m => m.Amount).HasColumnName("ValorTotal").IsRequired();
                    money.Property(m => m.Currency).HasColumnName("Moeda").HasMaxLength(3).IsRequired();
                });

            modelBuilder.Entity<ItemPedido>()
                .OwnsOne(i => i.Preco, money =>
                {
                    money.Property(m => m.Amount).HasColumnName("Preco").IsRequired();
                    money.Property(m => m.Currency).HasColumnName("PrecoMoeda").HasMaxLength(3).IsRequired();
                });
        }
    }
}