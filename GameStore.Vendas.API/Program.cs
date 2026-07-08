using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using GameStore.Common.Events;
using GameStore.Vendas.Infrastructure.Extensions;
using GameStore.Vendas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .Enrich.FromLogContext()
    .Enrich.WithSpan()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithSpan()
    .Enrich.WithProperty("ServiceName", "vendas-api")
    .WriteTo.Console(new CompactJsonFormatter()));

// Configure Kestrel to listen on port 80
builder.WebHost.UseUrls("http://*:80");

// appsettings.json / appsettings.{Environment}.json / variáveis de ambiente já são
// carregados por WebApplication.CreateBuilder() na ordem correta de precedência
// (env vars por último = maior prioridade). Re-adicionar os arquivos JSON aqui
// os colocava DEPOIS das env vars na cadeia de configuração, fazendo o valor
// hardcoded de appsettings.Development.json (localhost) sobrescrever a
// ConnectionStrings__DefaultConnection do docker-compose.yml.

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

builder.Services.AddVendasInfrastructure(builder.Configuration);

// Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// AddSecurityDefinition/Requirement expõem o botão "Authorize" (JWT Bearer) no Swagger UI —
// sem isso não há como testar os endpoints [Authorize] de /api/pedidos via Try-it-out.
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Prometheus Metrics
builder.Services.AddSingleton<IMetricServer>(new KestrelMetricServer(port: 9093));

// OpenTelemetry — Distributed Tracing (fase3-T08)
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
var sampleRatio = double.TryParse(builder.Configuration["OTEL_TRACES_SAMPLER_ARG"], out var configuredRatio)
    ? configuredRatio
    : 1.0;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("vendas-api"))
    .WithTracing(t => t
        .SetSampler(new TraceIdRatioBasedSampler(sampleRatio))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        // Spans de publish/consume do RabbitMqAdapter/BaseEventConsumer (GameStore.Common) —
        // sem isso, o trace_id propagado via TraceContextPropagator não aparece no backend.
        .AddSource("GameStore.Common.Messaging")
        .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "VendasAPI";

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Sem isso, o ASP.NET Core remapeia claims JWT curtas para URIs longas
        // (ex: "sub" -> ClaimTypes.NameIdentifier), quebrando User.FindFirst("sub")
        // usado em PedidoController (mesmo problema documentado em GameStore.Usuarios.API).
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false, // Accept tokens from other issuers (inter-service communication)
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Start Prometheus Metrics Server
var metricsServer = app.Services.GetRequiredService<IMetricServer>();
_ = metricsServer; // Ensures server is started

// Força a construção do IEventBus (RabbitMqAdapter) na inicialização em vez de na primeira
// requisição — sem isso, o exchange e as filas fan-out ("notificacoes.pedido-finalizado",
// "pagamentos.pedido-finalizado") só seriam declarados no primeiro "finalizar pedido", momento em
// que o GameStore.Notifications.Functions (RabbitMQTrigger) já teria falhado ao indexar por a fila
// ainda não existir.
var eventBus = app.Services.GetRequiredService<IEventBus>();
_ = eventBus;

// Aplica as migrations do EF Core na inicialização — cada microsserviço gerencia seu próprio
// schema (nenhum outro serviço acessa o banco de Vendas). Sem isso, um banco recém-criado
// (docker-compose de um ambiente novo) nunca teria as tabelas, e toda escrita falharia.
using (var migrationScope = app.Services.CreateScope())
{
    var vendasDbContext = migrationScope.ServiceProvider.GetRequiredService<VendasDbContext>();
    await vendasDbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Add Prometheus middleware for request/response metrics
app.UseHttpMetrics();

app.MapControllers();
app.MapMetrics();

// Health check endpoint for Kubernetes probes
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
