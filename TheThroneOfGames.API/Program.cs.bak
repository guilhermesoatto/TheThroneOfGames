using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TheThroneOfGames.Application.Interface;
using TheThroneOfGames.Application;
using TheThroneOfGames.Domain.Interfaces;
using TheThroneOfGames.Domain.Events;
using TheThroneOfGames.Infrastructure.Repository;
using TheThroneOfGames.Infrastructure.Events;
using Microsoft.AspNetCore.Builder;
using TheThroneOfGames.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using GameStore.Catalogo.Application.EventHandlers;
using GameStore.Usuarios.Application.EventHandlers;
using GameStore.Vendas.Application.EventHandlers;
using GameStore.Usuarios.Application.Handlers;
using GameStore.Catalogo.Application.Handlers;
using GameStore.Vendas.Application.Handlers;
using GameStore.Usuarios.Application.Queries;
using GameStore.Catalogo.Application.Queries;
using GameStore.Vendas.Application.Queries;
using GameStore.Usuarios.Application.Commands;
using GameStore.Catalogo.Application.Commands;
using GameStore.Vendas.Application.Commands;
using GameStore.Usuarios.Application.DTOs;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Vendas.Application.DTOs;
using GameStore.CQRS.Abstractions;

var builder = WebApplication.CreateBuilder(args);
// Register application services
builder.Services.AddDbContext<MainDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

#region Injeção de dependencias
// Add application services
builder.Services.AddApplicationServices();

// Event Bus - Barramento de eventos de domínio
builder.Services.AddSingleton<IEventBus, SimpleEventBus>();

// Register event handlers for cross-context communication
var eventBus = new SimpleEventBus();
builder.Services.AddSingleton<IEventBus>(eventBus);

// Subscribe handlers to events
eventBus.Subscribe<UsuarioAtivadoEvent>(new UsuarioAtivadoEventHandler());
eventBus.Subscribe<GameCompradoEvent>(new GameCompradoEventHandler());
eventBus.Subscribe<PedidoFinalizadoEvent>(new PedidoFinalizadoEventHandler());

// Authentication & email
builder.Services.AddScoped<TheThroneOfGames.API.Services.AuthenticationService>();
builder.Services.AddScoped<TheThroneOfGames.Infrastructure.ExternalServices.EmailService>();

// Command Handlers - CQRS Pattern
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<ActivateUserCommand>, ActivateUserCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<UpdateUserProfileCommand>, UpdateUserProfileCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<CreateUserCommand>, CreateUserCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<ChangeUserRoleCommand>, ChangeUserRoleCommandHandler>();

builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<CreateGameCommand>, CreateGameCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<UpdateGameCommand>, UpdateGameCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<RemoveGameCommand>, RemoveGameCommandHandler>();

builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<CreatePurchaseCommand>, CreatePurchaseCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<FinalizePurchaseCommand>, FinalizePurchaseCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<CancelPurchaseCommand>, CancelPurchaseCommandHandler>();

// Query Handlers - CQRS Pattern
// TODO: Fix Usuario Query Handlers - temporarily commented
// builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetUserByIdQuery, UsuarioDTO?>, GameStore.Usuarios.Application.Queries.GetUserByIdQueryHandler>();
// builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetUserByEmailQuery, UsuarioDTO?>, GameStore.Usuarios.Application.Queries.GetUserByEmailQueryHandler>();
// builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetAllUsersQuery, IEnumerable<UsuarioDTO>>, GameStore.Usuarios.Application.Queries.GetAllUsersQueryHandler>();
// builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetUsersByRoleQuery, IEnumerable<UsuarioDTO>>, GameStore.Usuarios.Application.Queries.GetUsersByRoleQueryHandler>();
// builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetActiveUsersQuery, IEnumerable<UsuarioDTO>>, GameStore.Usuarios.Application.Queries.GetActiveUsersQueryHandler>();
// builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<CheckEmailExistsQuery, bool>, GameStore.Usuarios.Application.Queries.CheckEmailExistsQueryHandler>();

builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetGameByIdQuery, GameDTO?>, GetGameByIdQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetGameByNameQuery, GameDTO?>, GetGameByNameQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetAllGamesQuery, IEnumerable<GameDTO>>, GetAllGamesQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetGamesByGenreQuery, IEnumerable<GameDTO>>, GetGamesByGenreQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetAvailableGamesQuery, IEnumerable<GameDTO>>, GetAvailableGamesQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetGamesByPriceRangeQuery, IEnumerable<GameDTO>>, GetGamesByPriceRangeQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<SearchGamesQuery, IEnumerable<GameDTO>>, SearchGamesQueryHandler>();

builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetPurchaseByIdQuery, PurchaseDTO?>, GameStore.Vendas.Application.Handlers.GetPurchaseByIdQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetPurchasesByUserQuery, IEnumerable<PurchaseDTO>>, GameStore.Vendas.Application.Handlers.GetPurchasesByUserQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetAllPurchasesQuery, IEnumerable<PurchaseDTO>>, GameStore.Vendas.Application.Handlers.GetAllPurchasesQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetPurchasesByStatusQuery, IEnumerable<PurchaseDTO>>, GameStore.Vendas.Application.Handlers.GetPurchasesByStatusQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetPurchasesByDateRangeQuery, IEnumerable<PurchaseDTO>>, GameStore.Vendas.Application.Handlers.GetPurchasesByDateRangeQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetPurchasesByGameQuery, IEnumerable<PurchaseDTO>>, GameStore.Vendas.Application.Handlers.GetPurchasesByGameQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GetSalesStatsQuery, SalesStatsDTO>, GameStore.Vendas.Application.Handlers.GetSalesStatsQueryHandler>();
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


