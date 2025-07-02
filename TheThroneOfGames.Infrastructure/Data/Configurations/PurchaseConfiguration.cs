using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.Infrastructure.Data.Configurations
{
    public class PurchaseConfiguration : IEntityTypeConfiguration<PurchaseEntity>
    {
        public void Configure(EntityTypeBuilder<PurchaseEntity> builder)
        {
            builder.ToTable("Purchases");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.UserId).IsRequired();
            builder.Property(p => p.PurchaseDate).IsRequired();
            builder.Property(p => p.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();

            // Configuração do relacionamento muitos-para-muitos entre Purchase e Game
            builder
                .HasMany(p => p.Games)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "PurchaseGames",
                    j => j.HasOne<GameEntity>().WithMany().HasForeignKey("GameId"),
                    j => j.HasOne<PurchaseEntity>().WithMany().HasForeignKey("PurchaseId"));
        }
    }
}
