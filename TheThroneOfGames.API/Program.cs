using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TheThroneOfGames.Domain.Events;
using TheThroneOfGames.Infrastructure.Events;
using Microsoft.AspNetCore.Builder;
using TheThroneOfGames.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using GameStore.Catalogo.Application.EventHandlers;
using GameStore.Usuarios.Application.EventHandlers;
// handler namespaces intentionally referenced by fully-qualified names to avoid ambiguity with Query handlers
using GameStore.Usuarios.Application.Queries;
using GameStore.Catalogo.Application.Queries;
using CQRS = GameStore.CQRS.Abstractions;
using GameStore.Usuarios.Application.Commands;
using GameStore.Catalogo.Application.Commands;
using GameStore.Vendas.Application.Commands;
using GameStore.Usuarios.Application.DTOs;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Vendas.Application.DTOs;
using GameStore.Usuarios.Infrastructure.Extensions;
using GameStore.Catalogo.Infrastructure.Extensions;
using GameStore.Vendas.Application.Extensions;
using GameStore.Vendas.Infrastructure.Extensions;
using TheThroneOfGames.API.Extensions;
using TheThroneOfGames.Application;


var builder = WebApplication.CreateBuilder(args);

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection is not configured.");

// Register application services - sempre usar SQL Server (inclusive em testes)
builder.Services.AddDbContext<MainDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add bounded contexts
builder.Services.AddUsuariosContext(connectionString);
builder.Services.AddCatalogoContext(connectionString);
builder.Services.AddVendasApplication();
builder.Services.AddVendasInfrastructure(builder.Configuration);

// Add legacy application services (for Admin controllers)
builder.Services.AddApplicationServices();

// Event Bus - Barramento de eventos de domínio
builder.Services.AddEventBus(builder.Configuration);

// Register event handlers for cross-context communication (only for SimpleEventBus)
var useRabbitMq = builder.Configuration.GetValue<bool>("EventBus:UseRabbitMq", false);
if (!useRabbitMq)
{
    var eventBus = new GameStore.Common.Messaging.SimpleEventBus();
    builder.Services.AddSingleton<GameStore.Common.Events.IEventBus>(eventBus);

    // Subscribe handlers to events
    eventBus.Subscribe<GameStore.Common.Events.UsuarioAtivadoEvent>(new GameStore.Catalogo.Application.EventHandlers.UsuarioAtivadoEventHandler());
    eventBus.Subscribe<GameStore.Common.Events.UsuarioAtivadoEvent>(new GameStore.Usuarios.Application.EventHandlers.UsuarioAtivadoEventHandler());
    eventBus.Subscribe<GameStore.Common.Events.GameCompradoEvent>(new GameStore.Usuarios.Application.EventHandlers.GameCompradoEventHandler());
}

// Authentication & email
builder.Services.AddScoped<TheThroneOfGames.Infrastructure.ExternalServices.EmailService>();

// Command Handlers - CQRS Pattern
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Usuarios.Application.Commands.ActivateUserCommand>, GameStore.Usuarios.Application.Handlers.ActivateUserCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Usuarios.Application.Commands.UpdateUserProfileCommand>, GameStore.Usuarios.Application.Handlers.UpdateUserProfileCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Usuarios.Application.Commands.CreateUserCommand>, GameStore.Usuarios.Application.Handlers.CreateUserCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Usuarios.Application.Commands.ChangeUserRoleCommand>, GameStore.Usuarios.Application.Handlers.ChangeUserRoleCommandHandler>();

builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Catalogo.Application.Commands.CreateGameCommand>, GameStore.Catalogo.Application.Handlers.CreateGameCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Catalogo.Application.Commands.UpdateGameCommand>, GameStore.Catalogo.Application.Handlers.UpdateGameCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Catalogo.Application.Commands.RemoveGameCommand>, GameStore.Catalogo.Application.Handlers.RemoveGameCommandHandler>();

// Query Handlers - CQRS Pattern
// Query Handlers - CQRS Pattern
// NOTE: user Query types/handlers currently live under the separate folder `GameStore.Usuarios.Application` and
// are not included in the `GameStore.Usuarios` project referenced by the API. Skip registering those handlers
// here until those types are compiled into a referenced project. See docs/ for guidance.

builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.GetGameByIdQuery, GameStore.Catalogo.Application.DTOs.GameDTO?>, GameStore.Catalogo.Application.Queries.GetGameByIdQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.GetGameByNameQuery, GameStore.Catalogo.Application.DTOs.GameDTO?>, GameStore.Catalogo.Application.Queries.GetGameByNameQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.GetAllGamesQuery, IEnumerable<GameStore.Catalogo.Application.DTOs.GameDTO>>, GameStore.Catalogo.Application.Queries.GetAllGamesQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.GetGamesByGenreQuery, IEnumerable<GameStore.Catalogo.Application.DTOs.GameDTO>>, GameStore.Catalogo.Application.Queries.GetGamesByGenreQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.GetAvailableGamesQuery, IEnumerable<GameStore.Catalogo.Application.DTOs.GameDTO>>, GameStore.Catalogo.Application.Queries.GetAvailableGamesQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.GetGamesByPriceRangeQuery, IEnumerable<GameStore.Catalogo.Application.DTOs.GameDTO>>, GameStore.Catalogo.Application.Queries.GetGamesByPriceRangeQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.SearchGamesQuery, IEnumerable<GameStore.Catalogo.Application.DTOs.GameDTO>>, GameStore.Catalogo.Application.Queries.SearchGamesQueryHandler>();


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
// Configure o pipeline HTTP para usar Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); 
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TheThroneOfGames API v1"));
}

app.UseHttpsRedirection();

// Global exception handling middleware
app.UseMiddleware<TheThroneOfGames.API.Middleware.ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // Garante que os controllers sejam mapeados

app.Run(); // Mantém a aplicação rodando

public partial class Program { }


