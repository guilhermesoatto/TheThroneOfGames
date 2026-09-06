using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using TheThroneOfGames.Application;
using TheThroneOfGames.Domain.Events;
using TheThroneOfGames.Infrastructure.Events;
using TheThroneOfGames.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection is not configured.");

// Register application services - sempre usar SQL Server (inclusive em testes)
builder.Services.AddDbContext<MainDbContext>(options =>
    options.UseSqlServer(connectionString));

// Application services (Fase 2 - Monolito)
builder.Services.AddApplicationServices();
builder.Services.AddScoped<TheThroneOfGames.API.Services.AuthenticationService>();

// Event Bus - Barramento de eventos de domínio (in-memory, monolito)
builder.Services.AddSingleton<IEventBus, SimpleEventBus>();

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT 'Key' is not configured.")))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers(); // Adiciona suporte a controllers

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Opcional: Personalizar a documenta��o do Swagger
    // Voc� pode adicionar informa��es sobre sua API aqui.
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Minha Super Minimal API", // T�tulo da sua API
        Version = "v1", // Vers�o da API
        Description = "Uma API de exemplo para gerenciar produtos e clientes.", // Descri��o
        TermsOfService = new Uri("https://example.com/terms"), // Termos de Servi�o (opcional)
        Contact = new Microsoft.OpenApi.Models.OpenApiContact // Contato (opcional)
        {
            Name = "Seu Nome",
            Email = "seu.email@example.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense // Licen�a (opcional)
        {
            Name = "Licen�a MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }

    });
    // Add JWT Bearer authentication to Swagger (Authorize button)
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Insira o token JWT no campo: 'Bearer {token}'\n\nExemplo: 'Bearer eyJhbGci...'.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// Aplica as migrations pendentes automaticamente no startup (necessário para
// `docker-compose up` funcionar em um ambiente limpo, sem passo manual de `dotnet ef database update`).
// Só faz sentido em provider relacional — nos testes (WebApplicationFactory + EF InMemory)
// o provider não é relacional e `Migrate()` lançaria exceção.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
}

// Configure o pipeline HTTP para usar Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TheThroneOfGames API v1"));
}

app.UseHttpsRedirection();

// Global exception handling middleware
app.UseMiddleware<TheThroneOfGames.API.Middleware.ExceptionMiddleware>();

// Métricas Prometheus (scrape em /metrics, ver monitoring/prometheus.yml)
app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // Garante que os controllers sejam mapeados
app.MapMetrics(); // Expõe /metrics para o Prometheus

app.Run(); // Mantém a aplicação rodando

public partial class Program { }
