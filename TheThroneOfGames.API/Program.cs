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
// handler namespaces intentionally referenced by fully-qualified names to avoid ambiguity with Query handlers
using GameStore.Usuarios.Application.Queries;
using GameStore.Catalogo.Application.Queries;
using GameStore.Vendas.Application.Queries;
using CQRS = GameStore.CQRS.Abstractions;
using GameStore.Usuarios.Application.Commands;
using GameStore.Catalogo.Application.Commands;
using GameStore.Vendas.Application.Commands;
using GameStore.Usuarios.Application.DTOs;
using GameStore.Catalogo.Application.DTOs;
using GameStore.Vendas.Application.DTOs;
using GameStore.Common.Messaging;


var builder = WebApplication.CreateBuilder(args);
// Register application services
builder.Services.AddDbContext<MainDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

#region Injeção de dependencias
// Add application services
builder.Services.AddApplicationServices();

// Event Bus - Barramento de eventos de domínio (with fallback to SimpleEventBus if RabbitMQ unavailable)
IEventBus eventBusInstance;
try
{
    // Try to use RabbitMQ adapter
    builder.Services.AddRabbitMqEventBus(builder.Configuration);
    var provider = builder.Services.BuildServiceProvider();
    eventBusInstance = provider.GetRequiredService<IEventBus>();
    Console.WriteLine("✓ RabbitMQ adapter registered successfully");
}
catch (Exception ex)
{
    // Fallback to SimpleEventBus for development/testing
    Console.WriteLine($"⚠ RabbitMQ connection failed: {ex.Message}. Using SimpleEventBus fallback.");
    var simpleEventBus = new SimpleEventBus();
    builder.Services.AddSingleton<IEventBus>(simpleEventBus);
    eventBusInstance = simpleEventBus;
}

// Register event handlers for cross-context communication (only if using SimpleEventBus)
if (eventBusInstance is SimpleEventBus simpleEventBusInstance)
{
    simpleEventBusInstance.Subscribe<UsuarioAtivadoEvent>(new GameStore.Catalogo.Application.EventHandlers.UsuarioAtivadoEventHandler());
    simpleEventBusInstance.Subscribe<UsuarioAtivadoEvent>(new GameStore.Usuarios.Application.EventHandlers.UsuarioAtivadoEventHandler());
    simpleEventBusInstance.Subscribe<GameCompradoEvent>(new GameCompradoEventHandler());
    simpleEventBusInstance.Subscribe<PedidoFinalizadoEvent>(new PedidoFinalizadoEventHandler());
    Console.WriteLine("✓ SimpleEventBus handlers registered");
}

// Authentication & email
builder.Services.AddScoped<TheThroneOfGames.API.Services.AuthenticationService>();
builder.Services.AddScoped<TheThroneOfGames.Infrastructure.ExternalServices.EmailService>();

// Command Handlers - CQRS Pattern
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Usuarios.Application.Commands.ActivateUserCommand>, GameStore.Usuarios.Application.Handlers.ActivateUserCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Usuarios.Application.Commands.UpdateUserProfileCommand>, GameStore.Usuarios.Application.Handlers.UpdateUserProfileCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Usuarios.Application.Commands.CreateUserCommand>, GameStore.Usuarios.Application.Handlers.CreateUserCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Usuarios.Application.Commands.ChangeUserRoleCommand>, GameStore.Usuarios.Application.Handlers.ChangeUserRoleCommandHandler>();

builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Catalogo.Application.Commands.CreateGameCommand>, GameStore.Catalogo.Application.Handlers.CreateGameCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Catalogo.Application.Commands.UpdateGameCommand>, GameStore.Catalogo.Application.Handlers.UpdateGameCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Catalogo.Application.Commands.RemoveGameCommand>, GameStore.Catalogo.Application.Handlers.RemoveGameCommandHandler>();

builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Vendas.Application.Commands.CreatePurchaseCommand>, GameStore.Vendas.Application.Handlers.CreatePurchaseCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Vendas.Application.Commands.FinalizePurchaseCommand>, GameStore.Vendas.Application.Handlers.FinalizePurchaseCommandHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.ICommandHandler<GameStore.Vendas.Application.Commands.CancelPurchaseCommand>, GameStore.Vendas.Application.Handlers.CancelPurchaseCommandHandler>();

// Query Handlers - CQRS Pattern
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.GetGameByIdQuery, GameStore.Catalogo.Application.DTOs.GameDTO?>, GameStore.Catalogo.Application.Queries.GetGameByIdQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.GetGameByNameQuery, GameStore.Catalogo.Application.DTOs.GameDTO?>, GameStore.Catalogo.Application.Queries.GetGameByNameQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.GetAllGamesQuery, IEnumerable<GameStore.Catalogo.Application.DTOs.GameDTO>>, GameStore.Catalogo.Application.Queries.GetAllGamesQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.GetGamesByGenreQuery, IEnumerable<GameStore.Catalogo.Application.DTOs.GameDTO>>, GameStore.Catalogo.Application.Queries.GetGamesByGenreQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.GetAvailableGamesQuery, IEnumerable<GameStore.Catalogo.Application.DTOs.GameDTO>>, GameStore.Catalogo.Application.Queries.GetAvailableGamesQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.GetGamesByPriceRangeQuery, IEnumerable<GameStore.Catalogo.Application.DTOs.GameDTO>>, GameStore.Catalogo.Application.Queries.GetGamesByPriceRangeQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Catalogo.Application.Queries.SearchGamesQuery, IEnumerable<GameStore.Catalogo.Application.DTOs.GameDTO>>, GameStore.Catalogo.Application.Queries.SearchGamesQueryHandler>();

builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Vendas.Application.Queries.GetPurchaseByIdQuery, GameStore.Vendas.Application.DTOs.PurchaseDTO?>, GameStore.Vendas.Application.Handlers.GetPurchaseByIdQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Vendas.Application.Queries.GetPurchasesByUserQuery, IEnumerable<GameStore.Vendas.Application.DTOs.PurchaseDTO>>, GameStore.Vendas.Application.Handlers.GetPurchasesByUserQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Vendas.Application.Queries.GetAllPurchasesQuery, IEnumerable<GameStore.Vendas.Application.DTOs.PurchaseDTO>>, GameStore.Vendas.Application.Handlers.GetAllPurchasesQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Vendas.Application.Queries.GetPurchasesByStatusQuery, IEnumerable<GameStore.Vendas.Application.DTOs.PurchaseDTO>>, GameStore.Vendas.Application.Handlers.GetPurchasesByStatusQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Vendas.Application.Queries.GetPurchasesByDateRangeQuery, IEnumerable<GameStore.Vendas.Application.DTOs.PurchaseDTO>>, GameStore.Vendas.Application.Handlers.GetPurchasesByDateRangeQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Vendas.Application.Queries.GetPurchasesByGameQuery, IEnumerable<GameStore.Vendas.Application.DTOs.PurchaseDTO>>, GameStore.Vendas.Application.Handlers.GetPurchasesByGameQueryHandler>();
builder.Services.AddScoped<GameStore.CQRS.Abstractions.IQueryHandler<GameStore.Vendas.Application.Queries.GetSalesStatsQuery, GameStore.Vendas.Application.Queries.SalesStatsDTO>, GameStore.Vendas.Application.Handlers.GetSalesStatsQueryHandler>();
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
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TheThroneOfGames API",
        Version = "v1",
        Description = "API para gerenciar usuários, catálogo de jogos e vendas.",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Support",
            Email = "support@example.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Insira o token JWT no campo: 'Bearer {token}'",
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TheThroneOfGames API v1"));
}

app.UseHttpsRedirection();
app.UseMiddleware<TheThroneOfGames.API.Middleware.ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
