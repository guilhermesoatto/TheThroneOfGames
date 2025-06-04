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
    public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
    {
        public void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            builder.ToTable("Users"); 

            builder.HasKey(u => u.Id); 

            builder.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasIndex(u => u.Email)
                .IsUnique(); // Garante que não haverá e-mails duplicados

            builder.Property(u => u.Password)
                .IsRequired();

            builder.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(50); // Ajuste o tamanho conforme necessário

            builder.Property(u => u.IsActive)
                .IsRequired()
                .HasDefaultValue(false); // Usuários começam inativos

            builder.Property(u => u.ActiveToken) 
                .HasMaxLength(255); 

            builder.Property(u => u.Nickname)
               .HasMaxLength(50); // Pode ser opcional no início ou obrigatório após ativação

            // Adicionar propriedade RegistrationDate (para rastreamento)
            // Se RegistrationDate ainda não existe na classe TheThroneOfGames.Infrastructure.Entities.Usuario, adicione-a:
            // public DateTime RegistrationDate { get; set; }
            // builder.Property(u => u.RegistrationDate)
            //    .IsRequired();

            // 
            /*
            builder.HasData(
                new Usuario(
                    name: "Admin User", // Adicione o parâmetro nome se o construtor exigir
                    email: "admin@thethroneofgames.com",
                    passwordHash: "SENHA_HASH_ADMIN_AQUI", // Gere um hash real depois!
                    role: "Admin",
                    activeToken: null // Admin pode ser ativo por padrão ou não precisar de token
                )
                {
                     // Se Id não é definido no construtor ou precisa ser fixo para seed:
                     // Id = Guid.Parse("seu-guid-fixo-para-admin"),
                     // IsActive = true, // Admin já começa ativo
                     // RegistrationDate = DateTime.UtcNow
                }
            );
            */
        }
    }
}
