using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheThroneOfGames.Infrastructure.Data.Configurations
{
    public class PurchaseConfiguration : IEntityTypeConfiguration<PurchaseEntity>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PurchaseEntity> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.UserId).IsRequired();
            builder.Property(p => p.GameId).IsRequired();
            builder.Property(p => p.PurchaseDate).IsRequired();
            builder.Property(p => p.Price).HasColumnType("decimal(18,2)").IsRequired();
        }
    }
    {
    }
}
