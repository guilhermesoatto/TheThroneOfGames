using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Application;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Infrastructure.Repository;
using Microsoft.AspNetCore.Builder;


var builder = WebApplication.CreateBuilder(args);
// Register application services
#region Injeção de dependencias
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
#endregion


// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers(); // Adiciona suporte a controllers

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Opcional: Personalizar a documentação do Swagger
    // Você pode adicionar informações sobre sua API aqui.
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Minha Super Minimal API", // Título da sua API
        Version = "v1", // Versão da API
        Description = "Uma API de exemplo para gerenciar produtos e clientes.", // Descrição
        TermsOfService = new Uri("https://example.com/terms"), // Termos de Serviço (opcional)
        Contact = new Microsoft.OpenApi.Models.OpenApiContact // Contato (opcional)
        {
            Name = "Seu Nome",
            Email = "seu.email@example.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense // Licença (opcional)
        {
            Name = "Licença MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }

    });
});

var app = builder.Build();

// Configure o pipeline HTTP para usar Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); 
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TheThroneOfGames API v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // Garante que os controllers sejam mapeados

app.Run(); // Mantém a aplicação rodando


