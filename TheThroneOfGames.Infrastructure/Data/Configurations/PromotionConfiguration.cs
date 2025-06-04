using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheThroneOfGames.Infrastructure.Entities;

namespace TheThroneOfGames.Infrastructure.Data.Configurations
{
    public class PromotionConfiguration : IEntityTypeConfiguration<PromotionEntity>
    {
        public void Configure(EntityTypeBuilder<PromotionEntity> builder)
        {
            builder.ToTable("Promotions");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Discount)
                .IsRequired();

            builder.Property(p => p.ValidUntil)
                .IsRequired();

            builder.Property(p => p.Description)
                .HasMaxLength(500);

            builder.HasMany<GameEntity>()
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "PromotionGames",
                    j => j.HasOne<GameEntity>().WithMany().HasForeignKey("GameId"),
                    j => j.HasOne<PromotionEntity>().WithMany().HasForeignKey("PromotionId"));
        }
    }
}
