using Microsoft.EntityFrameworkCore;
using TheThroneOfGames.Domain.Entities;

namespace TheThroneOfGames.Infrastructure.Data.Configurations
{
    public class PurchaseConfiguration : IEntityTypeConfiguration<PurchaseEntity>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PurchaseEntity> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.UserId).IsRequired();
            builder.Property(p => p.PurchaseDate).IsRequired();
            builder.Property(p => p.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();
        }
    }
}
